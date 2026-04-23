// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "@openzeppelin/contracts/access/Ownable.sol";

contract LandRegistry is ERC721, Ownable {
    event TileClaimed(address indexed owner, int16 x, int16 y, uint256 tokenId);
    event TileReleased(address indexed owner, int16 x, int16 y, uint256 tokenId);

    mapping(bytes32 => address) public tileOwner;
    mapping(uint256 => int16[2]) public tokenCoords;

    uint256 private _nextTokenId;

    constructor(address initialOwner) ERC721("NomadLand", "LAND") Ownable(initialOwner) {}

    function tileKey(int16 x, int16 y) public pure returns (bytes32) {
        return keccak256(abi.encodePacked(x, y));
    }

    function claimTile(int16 x, int16 y) external returns (uint256 tokenId) {
        bytes32 key = tileKey(x, y);
        require(tileOwner[key] == address(0), "Tile already claimed");

        tokenId = _nextTokenId++;
        _mint(msg.sender, tokenId);
        tileOwner[key] = msg.sender;
        tokenCoords[tokenId] = [x, y];

        emit TileClaimed(msg.sender, x, y, tokenId);
    }

    function releaseTile(uint256 tokenId) external {
        require(ownerOf(tokenId) == msg.sender, "Not tile owner");
        int16[2] memory coords = tokenCoords[tokenId];
        bytes32 key = tileKey(coords[0], coords[1]);
        tileOwner[key] = address(0);
        delete tokenCoords[tokenId];
        _burn(tokenId);
        emit TileReleased(msg.sender, coords[0], coords[1], tokenId);
    }

    function isClaimed(int16 x, int16 y) external view returns (bool) {
        return tileOwner[tileKey(x, y)] != address(0);
    }

    function totalMinted() external view returns (uint256) {
        return _nextTokenId;
    }
}
