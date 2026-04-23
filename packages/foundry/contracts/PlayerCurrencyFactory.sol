// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

import "@openzeppelin/contracts/token/ERC20/ERC20.sol";

contract PlayerCurrency is ERC20 {
    address public immutable creator;
    string public description;

    constructor(string memory name, string memory symbol, uint256 initialSupply, address _creator, string memory _description)
        ERC20(name, symbol)
    {
        creator = _creator;
        description = _description;
        _mint(_creator, initialSupply * 10 ** decimals());
    }
}

contract PlayerCurrencyFactory {
    event CurrencyCreated(
        address indexed creator,
        address indexed tokenAddress,
        string name,
        string symbol,
        uint256 initialSupply
    );

    address[] public allCurrencies;
    mapping(address => address[]) public currenciesByCreator;

    function createCurrency(
        string calldata name,
        string calldata symbol,
        uint256 initialSupply,
        string calldata description
    ) external returns (address tokenAddress) {
        PlayerCurrency token = new PlayerCurrency(name, symbol, initialSupply, msg.sender, description);
        tokenAddress = address(token);
        allCurrencies.push(tokenAddress);
        currenciesByCreator[msg.sender].push(tokenAddress);
        emit CurrencyCreated(msg.sender, tokenAddress, name, symbol, initialSupply);
    }

    function getAllCurrencies() external view returns (address[] memory) {
        return allCurrencies;
    }

    function getCurrenciesByCreator(address creator) external view returns (address[] memory) {
        return currenciesByCreator[creator];
    }

    function currencyCount() external view returns (uint256) {
        return allCurrencies.length;
    }
}
