package com.monadrepublic.service;

import com.monadrepublic.config.BlockchainProperties;
import com.monadrepublic.domain.entity.WorldTile;
import com.monadrepublic.domain.entity.WorldTileId;
import com.monadrepublic.domain.repository.WorldTileRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.web3j.abi.FunctionEncoder;
import org.web3j.abi.datatypes.Function;
import org.web3j.abi.datatypes.generated.Int16;
import org.web3j.crypto.Credentials;
import org.web3j.crypto.RawTransaction;
import org.web3j.crypto.TransactionEncoder;
import org.web3j.protocol.Web3j;
import org.web3j.protocol.core.DefaultBlockParameterName;
import org.web3j.protocol.core.methods.response.EthSendTransaction;
import org.web3j.utils.Numeric;

import java.math.BigInteger;
import java.util.Collections;
import java.util.List;

@Service
@RequiredArgsConstructor
@Slf4j
public class WorldService {

    private final WorldTileRepository tileRepo;
    private final Web3j web3j;
    private final Credentials relayerCredentials;
    private final BlockchainProperties props;

    public List<WorldTile> getTilesInBounds(int x1, int y1, int x2, int y2) {
        return tileRepo.findInBounds(x1, y1, x2, y2);
    }

    @Transactional
    public String claimTile(String walletAddress, int x, int y) throws Exception {
        WorldTileId id = new WorldTileId(x, y);
        if (tileRepo.existsById(id)) {
            throw new IllegalStateException("Tile (" + x + "," + y + ") already claimed");
        }

        String txHash = submitClaimTx(x, y, walletAddress);

        WorldTile tile = new WorldTile(x, y);
        tile.setOwnerWallet(walletAddress);
        tileRepo.save(tile);

        return txHash;
    }

    @Transactional
    public void applyChainClaim(String ownerWallet, int x, int y, long tokenId, String txHash) {
        WorldTile tile = tileRepo.findById(new WorldTileId(x, y)).orElse(new WorldTile(x, y));
        tile.setOwnerWallet(ownerWallet);
        tile.setOnChainTokenId(tokenId);
        tileRepo.save(tile);
    }

    private String submitClaimTx(int x, int y, String callerAddress) throws Exception {
        String contractAddress = props.getContracts().getLandRegistry();
        if (contractAddress == null || contractAddress.isBlank()) {
            log.warn("LandRegistry address not configured, simulating TX");
            return "0xsimulated" + System.currentTimeMillis();
        }

        Function function = new Function(
                "claimTile",
                List.of(new Int16(BigInteger.valueOf(x)), new Int16(BigInteger.valueOf(y))),
                Collections.emptyList()
        );
        String encodedFunction = FunctionEncoder.encode(function);

        BigInteger nonce = web3j.ethGetTransactionCount(
                relayerCredentials.getAddress(), DefaultBlockParameterName.LATEST
        ).send().getTransactionCount();

        BigInteger gasPrice = web3j.ethGasPrice().send().getGasPrice();
        BigInteger gasLimit = BigInteger.valueOf(200_000L);

        RawTransaction rawTx = RawTransaction.createTransaction(
                BigInteger.valueOf(props.getChainId()), nonce, gasLimit, contractAddress, BigInteger.ZERO, encodedFunction
        );
        byte[] signedTx = TransactionEncoder.signMessage(rawTx, props.getChainId(), relayerCredentials);
        EthSendTransaction response = web3j.ethSendRawTransaction(Numeric.toHexString(signedTx)).send();

        if (response.hasError()) {
            throw new RuntimeException("TX error: " + response.getError().getMessage());
        }
        return response.getTransactionHash();
    }
}
