package com.monadrepublic.config;

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;

@Component
@ConfigurationProperties(prefix = "blockchain")
@Data
public class BlockchainProperties {
    private String rpcUrl = "https://testnet-rpc.monad.xyz";
    private long chainId = 10143L;
    private String relayerPrivateKey;

    private Contracts contracts = new Contracts();

    @Data
    public static class Contracts {
        private String landRegistry;
        private String currencyFactory;
        private String gameCore;
    }
}
