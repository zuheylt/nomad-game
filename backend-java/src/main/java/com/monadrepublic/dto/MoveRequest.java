package com.monadrepublic.dto;

import lombok.Data;

@Data
public class MoveRequest {
    private String wallet;
    private double x;
    private double y;
    private double z;
}
