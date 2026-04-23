package com.nomad.controller;

import com.nomad.domain.entity.GameEvent;
import com.nomad.domain.repository.GameEventRepository;
import com.nomad.service.AlienService;
import com.nomad.service.PlayerService;
import com.nomad.service.WorldService;
import lombok.RequiredArgsConstructor;
import org.springframework.messaging.simp.SimpMessagingTemplate;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;
import java.util.Random;

@RestController
@RequestMapping("/api/game")
@RequiredArgsConstructor
public class GameController {

    private final AlienService alienService;
    private final PlayerService playerService;
    private final WorldService worldService;
    private final GameEventRepository eventRepo;
    private final SimpMessagingTemplate messaging;

    @GetMapping("/status")
    public Map<String, Object> getStatus() {
        return alienService.getStatus((int) playerService.getPlayerCount());
    }

    @GetMapping("/players")
    public Object getOnlinePlayers() {
        return playerService.getOnlinePlayers();
    }

    @PostMapping("/players/register")
    public Object register(@RequestBody Map<String, String> body) {
        return playerService.registerOrGet(body.get("wallet"));
    }

    @PostMapping("/treason/assign")
    public Map<String, Object> assignTreason() {
        List<com.nomad.domain.entity.Player> players = playerService.getOnlinePlayers();
        if (players.isEmpty())
            return Map.of("error", "No online players to assign treason to");

        var target = players.get(new Random().nextInt(players.size()));
        String wallet = target.getWalletAddress();
        String payload = String.format(
            "{\"target\":\"%s\",\"mission\":\"Sabotage the railgun before the aliens arrive\"}",
            wallet);
        GameEvent event = GameEvent.of("TREASON_ASSIGNED", null, payload);
        eventRepo.save(event);
        messaging.convertAndSend("/topic/events", event);
        return Map.of("status", "assigned", "target", wallet);
    }

    @PostMapping("/death")
    public Map<String, Object> recordDeath(@RequestBody Map<String, String> body) {
        String victim = body.get("victim");
        String killer = body.get("killer");
        try {
            String txHash = worldService.recordDeathOnChain(victim, killer);
            String payload = String.format(
                "{\"victim\":\"%s\",\"killer\":\"%s\"}", victim, killer);
            GameEvent event = GameEvent.of("DEATH", txHash, payload);
            eventRepo.save(event);
            messaging.convertAndSend("/topic/events", event);
            return Map.of("status", "recorded", "txHash", txHash, "victim", victim);
        } catch (Exception e) {
            return Map.of("error", e.getMessage());
        }
    }
}
