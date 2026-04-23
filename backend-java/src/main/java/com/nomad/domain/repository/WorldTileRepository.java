package com.nomad.domain.repository;

import com.nomad.domain.entity.WorldTile;
import com.nomad.domain.entity.WorldTileId;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;

public interface WorldTileRepository extends JpaRepository<WorldTile, WorldTileId> {
    @Query("SELECT t FROM WorldTile t WHERE t.id.x BETWEEN :x1 AND :x2 AND t.id.y BETWEEN :y1 AND :y2")
    List<WorldTile> findInBounds(@Param("x1") int x1, @Param("y1") int y1,
                                  @Param("x2") int x2, @Param("y2") int y2);

    List<WorldTile> findByOwnerWallet(String ownerWallet);
}
