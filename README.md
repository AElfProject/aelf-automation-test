# aelf-automation-test
Automation test project for Aelf enterprise v1.0.0

## Config myget source URL to AElfProject
```
nuget sources Add -Name myget.org -Source https://www.myget.org/F/aelf-project-dev/api/v3/index.json
```

## Clone and build code
``` 
git clone https://github.com/AElfProject/aelf-automation-test
cd ./aelf-automation-test
git checkout performance-test
dotnet build --configuration Release -o ./build-test-dir
```

## Test scripts
- AElf.Automation.BasicTransactions
- AElf.Automation.RpcPerformance
- AElf.Automation.AccountCheck
- AElf.Automation.BlockCheck
- AElf.Automation.MixedTransaction

## How to run
Basic node Settings:

1.Prepare test contract MultiToken to default directory
```
mkdir -p ~/.local/share/aelf/contracts
cp AElf.Contracts.MultiToken.dll ~/.local/share/aelf/contracts
```
2.Add your nodes configuration setting files in directory ``bin/Debug/netcoreapp3.0/config``. All these information used to contract deployment proposal approve and prepare ELF token for transaction execution.
So you need to set node configurations and also you need to copy all nodes accounts into test directory ``bin/Debug/netcore3.0/aelf/keys``.    
- Running standalone node, you just need to add one node settings in configuration. 
- Running multiple nodes, you need to set all nodes information to configuration. Test cannot execute authority transactions without nodes setting.
 
```
{
  "RequireAuthority": true,
  "Nodes": [
    {
      "name": "stand-alone-node",
      "endpoint": "127.0.0.1:8000",
      "account": "",
      "password": ""
    }
  ],
  "NativeTokenSymbol" : "ELF",
  "DefaultPassword": "123"
}
```

### AElf.Automation.BasicTransactions
Basic Transactions testing, you can run basic transactions to test node stability and transaction execution tps.
Following are details about running step and how to configuration.

Set ``base-config.json`` to set transaction type and number of executions.
```
{
  "ServiceUrl": "127.0.0.1:8000",
  "InitAccount": "",
  "Password": "",
  "TransferAmount": ,
  "ExecuteMode": ,
  "ContractCount": ,
  "Times": 10,
  "TokenAddress": ""
}

```
ExecuteMode:
```
{
   UserTransfer = 1,
   ContractTransfer = 2,
   RandomContractTransfer = 3,
   CheckUserBalance = 4,
   CheckTxInfo = 5,
   CheckBlockInfo = 6
}

```
#### Usage:
```
dotnet AElf.Automation.BasicTransactions.dll
```

### AElf.Automation.RpcPerformance
Performance testing, you can run huge transactions to test node stability and transaction execution tps. 
Following are details about running step and how to configuration.

Set ``rpc-performance.json`` to set transaction type and number of executions.
```
{
    "GroupCount": ,
    "TransactionGroup": 10,
    "TransactionCount":100,
    "ServiceUrl": "",
    "Timeout": 300,
    "RandomSenderTransaction": true,
    "Duration": 1000,
    "ContractAddress": "",
    "TokenList": []
}

```
*Adpot GroupCount and TransactionCount number can control transaction sent number frequency.*      
*GroupCount*: how many threads to sent transaction.   
*TransactionCount*: how many transactions sent each time in one thread.
*TransactionGroup*: how many transaction group execute each time in one block. 
*ServiceUrl*: node web api address and port to start execution.      
*RandomSenderTransaction*: sent transaction with sender are random, lot of transaction groups with set this value as true.
*ContractAddress*: if you have already deployed MultiToken Contract, you can send transaction through this contract.

#### Usage:
```
dotnet AElf.Automation.RpcTransaction.dll
```
