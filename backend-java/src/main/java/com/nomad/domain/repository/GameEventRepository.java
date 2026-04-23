package com.nomad.domain.repository;

import com.nomad.domain.entity.GameEvent;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface GameEventRepository extends JpaRepository<GameEvent, Long> {
    List<GameEvent> findByOrderByTimestampDesc(Pageable pageable);
    boolean existsByTxHashAndEventType(String txHash, String eventType);
}
