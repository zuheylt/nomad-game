// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "./DeployHelpers.s.sol";
import { LandRegistry } from "../contracts/LandRegistry.sol";
import { PlayerCurrencyFactory } from "../contracts/PlayerCurrencyFactory.sol";
import { GameCore } from "../contracts/GameCore.sol";

contract DeployGameContracts is ScaffoldETHDeploy {
    function run() external ScaffoldEthDeployerRunner {
        // Alien arrives in 72 hours from deploy; season runs 7 days total
        uint256 alienArrival = block.timestamp + 72 hours;
        uint256 seasonDuration = 7 days;

        LandRegistry land = new LandRegistry(deployer);
        deployments.push(Deployment({ name: "LandRegistry", addr: address(land) }));

        PlayerCurrencyFactory factory = new PlayerCurrencyFactory();
        deployments.push(Deployment({ name: "PlayerCurrencyFactory", addr: address(factory) }));

        GameCore game = new GameCore(alienArrival, seasonDuration, deployer, deployer);
        deployments.push(Deployment({ name: "GameCore", addr: address(game) }));
    }
}
