package com.nomad.dto;

import lombok.Data;

@Data
public class ClaimTileRequest {
    private String wallet;
    private int x;
    private int y;
}
