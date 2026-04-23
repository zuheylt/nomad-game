package com.monadrepublic.controller;

import com.monadrepublic.domain.entity.GameEvent;
import com.monadrepublic.domain.repository.GameEventRepository;
import com.monadrepublic.dto.MoveRequest;
import com.monadrepublic.service.PlayerService;
import lombok.RequiredArgsConstructor;
import org.springframework.data.domain.PageRequest;
import org.springframework.messaging.handler.annotation.MessageMapping;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/events")
@RequiredArgsConstructor
public class EventController {

    private final GameEventRepository eventRepo;
    private final PlayerService playerService;

    @GetMapping
    public List<GameEvent> getRecentEvents(@RequestParam(defaultValue = "20") int limit) {
        return eventRepo.findByOrderByTimestampDesc(PageRequest.of(0, Math.min(limit, 100)));
    }
}

@Controller
class WebSocketController {
    private final PlayerService playerService;

    WebSocketController(PlayerService playerService) {
        this.playerService = playerService;
    }

    @MessageMapping("/move")
    public void handleMove(MoveRequest req) {
        playerService.updatePosition(req.getWallet(), req.getX(), req.getY(), req.getZ());
    }
}
