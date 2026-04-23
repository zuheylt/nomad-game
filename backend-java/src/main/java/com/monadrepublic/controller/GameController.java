package com.monadrepublic.controller;

import com.monadrepublic.service.AlienService;
import com.monadrepublic.service.PlayerService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

import java.util.Map;

@RestController
@RequestMapping("/api/game")
@RequiredArgsConstructor
public class GameController {

    private final AlienService alienService;
    private final PlayerService playerService;

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
}
