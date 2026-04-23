// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/access/Ownable.sol";

contract GameCore is Ownable {
    event PlayerDied(address indexed victim, address indexed killer, uint256 timestamp);
    event TreasonMissionAssigned(address indexed target, bytes32 missionHash);
    event TreasonCompleted(address indexed traitor, uint256 payout);
    event PrizeDeposited(address indexed depositor, uint256 amount);
    event SeasonResolved(bool aliensDefeated, uint256 prizePool);
    event DefenseContributed(address indexed player, string contribution, uint256 value);

    uint256 public immutable alienArrivalTime;
    uint256 public immutable seasonEndTime;
    bool public seasonResolved;
    bool public aliensDefeated;

    mapping(address => bytes32) public pendingTreasonMission;
    mapping(address => uint256) public assetValue; // updated by backend relayer
    address[] public registeredPlayers;
    mapping(address => bool) public isRegistered;

    // Authorized backend relayer
    address public relayer;

    constructor(uint256 _alienArrivalTime, uint256 _seasonDuration, address _relayer, address initialOwner)
        Ownable(initialOwner)
    {
        alienArrivalTime = _alienArrivalTime;
        seasonEndTime = _alienArrivalTime + _seasonDuration;
        relayer = _relayer;
    }

    modifier onlyRelayer() {
        require(msg.sender == relayer || msg.sender == owner(), "Not authorized");
        _;
    }

    function deposit() external payable {
        emit PrizeDeposited(msg.sender, msg.value);
    }

    function getTimeUntilAliens() external view returns (uint256) {
        if (block.timestamp >= alienArrivalTime) return 0;
        return alienArrivalTime - block.timestamp;
    }

    function getPrizePool() external view returns (uint256) {
        return address(this).balance;
    }

    function registerPlayer(address player) external onlyRelayer {
        if (!isRegistered[player]) {
            isRegistered[player] = true;
            registeredPlayers.push(player);
        }
    }

    function recordDeath(address victim, address killer) external onlyRelayer {
        emit PlayerDied(victim, killer, block.timestamp);
    }

    function contributeDefense(string calldata contribution) external payable {
        emit DefenseContributed(msg.sender, contribution, msg.value);
        if (msg.value > 0) {
            // defense contributions add to prize pool
        }
    }

    function updateAssetValue(address player, uint256 value) external onlyRelayer {
        if (!isRegistered[player]) {
            isRegistered[player] = true;
            registeredPlayers.push(player);
        }
        assetValue[player] = value;
    }

    // Owner assigns a secret treason mission to a player
    function assignTreasonMission(address target, bytes32 missionHash) external onlyOwner {
        pendingTreasonMission[target] = missionHash;
        emit TreasonMissionAssigned(target, missionHash);
    }

    // Traitor submits proof to claim payout (25% of prize pool)
    function completeTreasonMission(bytes32 proof) external {
        bytes32 expected = pendingTreasonMission[msg.sender];
        require(expected != bytes32(0), "No mission assigned");
        require(keccak256(abi.encodePacked(proof)) == expected, "Invalid proof");

        delete pendingTreasonMission[msg.sender];
        uint256 payout = address(this).balance / 4;
        (bool ok,) = msg.sender.call{ value: payout }("");
        require(ok, "Payout failed");
        emit TreasonCompleted(msg.sender, payout);
    }

    // Owner resolves season: distributes prize pool proportionally by asset value if aliens defeated
    function resolveSeason(bool _aliensDefeated) external onlyOwner {
        require(!seasonResolved, "Already resolved");
        seasonResolved = true;
        aliensDefeated = _aliensDefeated;
        uint256 pool = address(this).balance;

        emit SeasonResolved(_aliensDefeated, pool);

        if (_aliensDefeated && pool > 0) {
            uint256 totalValue = 0;
            for (uint256 i = 0; i < registeredPlayers.length; i++) {
                totalValue += assetValue[registeredPlayers[i]];
            }
            if (totalValue > 0) {
                for (uint256 i = 0; i < registeredPlayers.length; i++) {
                    address player = registeredPlayers[i];
                    uint256 share = (pool * assetValue[player]) / totalValue;
                    if (share > 0) {
                        (bool ok,) = player.call{ value: share }("");
                        require(ok, "Transfer failed");
                    }
                }
            }
        } else if (!_aliensDefeated && pool > 0) {
            // aliens win: funds go to owner (game treasury)
            (bool ok,) = owner().call{ value: pool }("");
            require(ok, "Treasury transfer failed");
        }
    }

    function setRelayer(address _relayer) external onlyOwner {
        relayer = _relayer;
    }

    receive() external payable {
        emit PrizeDeposited(msg.sender, msg.value);
    }
}
