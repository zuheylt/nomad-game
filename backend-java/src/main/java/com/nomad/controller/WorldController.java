package com.nomad.controller;

import com.nomad.domain.entity.WorldTile;
import com.nomad.dto.ClaimTileRequest;
import com.nomad.service.WorldService;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/world")
@RequiredArgsConstructor
public class WorldController {

    private final WorldService worldService;

    @GetMapping("/tiles")
    public List<WorldTile> getTiles(
            @RequestParam(defaultValue = "-10") int x1,
            @RequestParam(defaultValue = "-10") int y1,
            @RequestParam(defaultValue = "10") int x2,
            @RequestParam(defaultValue = "10") int y2) {
        return worldService.getTilesInBounds(x1, y1, x2, y2);
    }

    @PostMapping("/claim")
    public ResponseEntity<?> claimTile(@RequestBody ClaimTileRequest req) {
        try {
            String txHash = worldService.claimTile(req.getWallet(), req.getX(), req.getY());
            return ResponseEntity.ok(Map.of("txHash", txHash, "x", req.getX(), "y", req.getY()));
        } catch (IllegalStateException e) {
            return ResponseEntity.badRequest().body(Map.of("error", e.getMessage()));
        } catch (Exception e) {
            return ResponseEntity.internalServerError().body(Map.of("error", e.getMessage()));
        }
    }
}
