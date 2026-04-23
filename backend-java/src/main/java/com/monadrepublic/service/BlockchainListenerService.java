package com.monadrepublic.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.monadrepublic.config.BlockchainProperties;
import com.monadrepublic.domain.entity.GameEvent;
import com.monadrepublic.domain.repository.GameEventRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.web3j.protocol.Web3j;
import org.web3j.protocol.core.DefaultBlockParameter;
import org.web3j.protocol.core.DefaultBlockParameterName;
import org.web3j.protocol.core.methods.request.EthFilter;
import org.web3j.protocol.core.methods.response.EthLog;

import jakarta.annotation.PostConstruct;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

@Service
@RequiredArgsConstructor
@Slf4j
public class BlockchainListenerService {

    private final Web3j web3j;
    private final BlockchainProperties props;
    private final GameEventRepository eventRepo;
    private final SimpMessagingTemplate messaging;
    private final WorldService worldService;
    private final ObjectMapper objectMapper;

    // Keccak-256 of event signatures
    private static final String TILE_CLAIMED_TOPIC =
            "0x" + org.web3j.crypto.Hash.sha3String("TileClaimed(address,int16,int16,uint256)");
    private static final String CURRENCY_CREATED_TOPIC =
            "0x" + org.web3j.crypto.Hash.sha3String("CurrencyCreated(address,address,string,string,uint256)");
    private static final String PLAYER_DIED_TOPIC =
            "0x" + org.web3j.crypto.Hash.sha3String("PlayerDied(address,address,uint256)");
    private static final String TREASON_ASSIGNED_TOPIC =
            "0x" + org.web3j.crypto.Hash.sha3String("TreasonMissionAssigned(address,bytes32)");
    private static final String TREASON_COMPLETED_TOPIC =
            "0x" + org.web3j.crypto.Hash.sha3String("TreasonCompleted(address,uint256)");

    private BigInteger lastProcessedBlock = BigInteger.ZERO;

    @PostConstruct
    public void init() {
        try {
            lastProcessedBlock = web3j.ethBlockNumber().send().getBlockNumber();
            log.info("Blockchain listener initialized at block {}", lastProcessedBlock);
        } catch (Exception e) {
            log.warn("Could not fetch current block number: {}", e.getMessage());
        }
    }

    @Scheduled(fixedDelay = 5000)
    public void pollEvents() {
        try {
            BigInteger currentBlock = web3j.ethBlockNumber().send().getBlockNumber();
            if (currentBlock.compareTo(lastProcessedBlock) <= 0) return;

            List<String> addresses = getContractAddresses();
            if (addresses.isEmpty()) return;

            EthFilter filter = new EthFilter(
                    DefaultBlockParameter.valueOf(lastProcessedBlock.add(BigInteger.ONE)),
                    DefaultBlockParameter.valueOf(currentBlock),
                    addresses
            );

            EthLog ethLog = web3j.ethGetLogs(filter).send();
            for (EthLog.LogResult<?> result : ethLog.getLogs()) {
                if (result instanceof EthLog.LogObject logEntry) {
                    processLog(logEntry);
                }
            }
            lastProcessedBlock = currentBlock;
        } catch (Exception e) {
            log.warn("Event poll error: {}", e.getMessage());
        }
    }

    private void processLog(EthLog.LogObject logEntry) {
        if (logEntry.getTopics().isEmpty()) return;
        String topic = logEntry.getTopics().get(0);
        String txHash = logEntry.getTransactionHash();

        try {
            String eventType = resolveEventType(topic);
            if (eventType == null) return;
            if (eventRepo.existsByTxHashAndEventType(txHash, eventType)) return;

            String payload = objectMapper.writeValueAsString(Map.of(
                    "txHash", txHash,
                    "blockNumber", logEntry.getBlockNumber(),
                    "address", logEntry.getAddress(),
                    "topics", logEntry.getTopics(),
                    "data", logEntry.getData()
            ));

            GameEvent event = GameEvent.of(eventType, txHash, payload);
            event.setBlockNumber(logEntry.getBlockNumber().longValue());
            eventRepo.save(event);
            messaging.convertAndSend("/topic/events", event);
            log.info("New event: {} tx={}", eventType, txHash);
        } catch (Exception e) {
            log.warn("Failed to process log {}: {}", txHash, e.getMessage());
        }
    }

    private String resolveEventType(String topic) {
        return switch (topic) {
            case String t when t.equalsIgnoreCase(TILE_CLAIMED_TOPIC) -> "TILE_CLAIMED";
            case String t when t.equalsIgnoreCase(CURRENCY_CREATED_TOPIC) -> "CURRENCY_CREATED";
            case String t when t.equalsIgnoreCase(PLAYER_DIED_TOPIC) -> "DEATH";
            case String t when t.equalsIgnoreCase(TREASON_ASSIGNED_TOPIC) -> "TREASON_ASSIGNED";
            case String t when t.equalsIgnoreCase(TREASON_COMPLETED_TOPIC) -> "TREASON_COMPLETED";
            default -> null;
        };
    }

    private List<String> getContractAddresses() {
        List<String> addresses = new ArrayList<>();
        BlockchainProperties.Contracts c = props.getContracts();
        if (c.getLandRegistry() != null && !c.getLandRegistry().isBlank()) addresses.add(c.getLandRegistry());
        if (c.getCurrencyFactory() != null && !c.getCurrencyFactory().isBlank()) addresses.add(c.getCurrencyFactory());
        if (c.getGameCore() != null && !c.getGameCore().isBlank()) addresses.add(c.getGameCore());
        return addresses;
    }
}
