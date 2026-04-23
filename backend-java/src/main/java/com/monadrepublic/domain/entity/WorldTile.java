package com.monadrepublic.domain.entity;

import jakarta.persistence.*;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@Table(name = "world_tiles")
@Data
@NoArgsConstructor
public class WorldTile {
    @EmbeddedId
    private WorldTileId id;

    @Column(name = "owner_wallet", length = 42)
    private String ownerWallet;

    @Column(name = "building_type", length = 32)
    private String buildingType = "NONE";

    @Column(name = "resource_level")
    private int resourceLevel = 0;

    @Column(name = "on_chain_token_id")
    private Long onChainTokenId;

    public WorldTile(int x, int y) {
        this.id = new WorldTileId(x, y);
    }
}
