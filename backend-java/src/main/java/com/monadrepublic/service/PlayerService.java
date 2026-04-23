package com.monadrepublic.service;

import com.monadrepublic.domain.entity.Player;
import com.monadrepublic.domain.repository.PlayerRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.List;

@Service
@RequiredArgsConstructor
public class PlayerService {

    private final PlayerRepository playerRepo;
    private final SimpMessagingTemplate messaging;

    @Transactional
    public Player registerOrGet(String walletAddress) {
        return playerRepo.findById(walletAddress).orElseGet(() -> {
            Player p = new Player(walletAddress);
            return playerRepo.save(p);
        });
    }

    @Transactional
    public Player updatePosition(String walletAddress, double x, double y, double z) {
        Player player = registerOrGet(walletAddress);
        player.setX(x);
        player.setY(y);
        player.setZ(z);
        player.setLastSeen(Instant.now());
        Player saved = playerRepo.save(player);
        messaging.convertAndSend("/topic/players", getOnlinePlayers());
        return saved;
    }

    public List<Player> getOnlinePlayers() {
        return playerRepo.findByLastSeenAfter(Instant.now().minusSeconds(30));
    }

    public long getPlayerCount() {
        return playerRepo.count();
    }
}
