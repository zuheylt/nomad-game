package com.monadrepublic.domain.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.Instant;

@Entity
@Table(name = "players")
@Data
@NoArgsConstructor
public class Player {
    @Id
    @Column(name = "wallet_address", length = 42)
    private String walletAddress;

    private String username;
    private double x;
    private double y;
    private double z;

    @Column(name = "last_seen")
    private Instant lastSeen = Instant.now();

    @Column(name = "asset_value")
    private long assetValue = 0;

    @Column(name = "is_traitor")
    private boolean traitor = false;

    public Player(String walletAddress) {
        this.walletAddress = walletAddress;
        this.username = walletAddress.substring(0, 6) + "..." + walletAddress.substring(38);
    }
}
