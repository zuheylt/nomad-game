package com.monadrepublic.domain.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.Instant;

@Entity
@Table(name = "game_events")
@Data
@NoArgsConstructor
public class GameEvent {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "tx_hash", length = 66)
    private String txHash;

    @Column(name = "event_type", length = 32, nullable = false)
    private String eventType;

    @Column(columnDefinition = "TEXT")
    private String payload;

    @Column(name = "block_number")
    private Long blockNumber;

    @Column(nullable = false)
    private Instant timestamp = Instant.now();

    public static GameEvent of(String eventType, String txHash, String payload) {
        GameEvent e = new GameEvent();
        e.eventType = eventType;
        e.txHash = txHash;
        e.payload = payload;
        e.timestamp = Instant.now();
        return e;
    }
}
