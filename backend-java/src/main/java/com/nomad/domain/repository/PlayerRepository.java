package com.nomad.domain.repository;

import com.nomad.domain.entity.Player;
import org.springframework.data.jpa.repository.JpaRepository;

import java.time.Instant;
import java.util.List;

public interface PlayerRepository extends JpaRepository<Player, String> {
    List<Player> findByLastSeenAfter(Instant since);
}
