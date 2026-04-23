package com.monadrepublic.service;

import com.monadrepublic.config.BlockchainProperties;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.web3j.abi.FunctionEncoder;
import org.web3j.abi.FunctionReturnDecoder;
import org.web3j.abi.TypeReference;
import org.web3j.abi.datatypes.Function;
import org.web3j.abi.datatypes.Type;
import org.web3j.abi.datatypes.generated.Uint256;
import org.web3j.protocol.Web3j;
import org.web3j.protocol.core.DefaultBlockParameterName;
import org.web3j.protocol.core.methods.request.Transaction;
import org.web3j.protocol.core.methods.response.EthCall;

import java.math.BigInteger;
import java.time.Instant;
import java.util.Collections;
import java.util.List;
import java.util.Map;

@Service
@RequiredArgsConstructor
@Slf4j
public class AlienService {

    private final Web3j web3j;
    private final BlockchainProperties props;

    private volatile long cachedAlienArrival = Instant.now().plusSeconds(72 * 3600).getEpochSecond();
    private volatile long cachedPrizePool = 0L;
    private volatile long lastFetch = 0L;
    private static final long CACHE_TTL_MS = 30_000L;

    public Map<String, Object> getStatus(int playerCount) {
        refreshCacheIfNeeded();
        long now = Instant.now().getEpochSecond();
        long secondsLeft = Math.max(0, cachedAlienArrival - now);
        return Map.of(
                "alienArrivalTime", cachedAlienArrival,
                "secondsUntilAliens", secondsLeft,
                "prizePoolWei", cachedPrizePool,
                "playerCount", playerCount
        );
    }

    private void refreshCacheIfNeeded() {
        long now = System.currentTimeMillis();
        if (now - lastFetch < CACHE_TTL_MS) return;
        lastFetch = now;

        String contractAddress = props.getContracts().getGameCore();
        if (contractAddress == null || contractAddress.isBlank()) return;

        try {
            cachedAlienArrival = callUint256(contractAddress, "alienArrivalTime");
            cachedPrizePool = callUint256(contractAddress, "getPrizePool");
        } catch (Exception e) {
            log.warn("Failed to refresh alien status from chain: {}", e.getMessage());
        }
    }

    private long callUint256(String contract, String functionName) throws Exception {
        Function fn = new Function(functionName, Collections.emptyList(),
                List.of(new TypeReference<Uint256>() {}));
        String encoded = FunctionEncoder.encode(fn);
        EthCall result = web3j.ethCall(
                Transaction.createEthCallTransaction(null, contract, encoded),
                DefaultBlockParameterName.LATEST
        ).send();
        List<Type> decoded = FunctionReturnDecoder.decode(result.getValue(), fn.getOutputParameters());
        if (decoded.isEmpty()) return 0L;
        return ((BigInteger) decoded.get(0).getValue()).longValueExact();
    }
}
