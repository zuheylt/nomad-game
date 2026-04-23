package com.nomad.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.web3j.crypto.Credentials;
import org.web3j.protocol.Web3j;
import org.web3j.protocol.http.HttpService;
import org.web3j.tx.gas.DefaultGasProvider;

@Configuration
public class Web3jConfig {

    @Bean
    public Web3j web3j(BlockchainProperties props) {
        return Web3j.build(new HttpService(props.getRpcUrl()));
    }

    @Bean
    public Credentials relayerCredentials(BlockchainProperties props) {
        String pk = props.getRelayerPrivateKey();
        if (pk == null || pk.isBlank()) {
            // dev fallback: create a throwaway key (won't have funds)
            return Credentials.create("0x4c0883a69102937d6231471b5dbb6e538eba2ef2f6ea2cbe6d8369d0cb635d01");
        }
        return Credentials.create(pk);
    }

    @Bean
    public DefaultGasProvider gasProvider() {
        return new DefaultGasProvider();
    }
}
