# aelf-automation-test
Automation test project for AElf

## Config myget source URL to AElfProject
```
nuget sources Add -Name myget.org -Source https://www.myget.org/F/aelf-project-dev/api/v3/index.json
```

## Clone and build code
``` 
git clone https://github.com/AElfProject/aelf-automation-test
cd ./aelf-automation-test
dotnet build --configuration Release -o ./build-test-dir
```

## CLI tool
- AElfChain.Console

## Test scripts
- AElf.Automation.Contracts.ScenarioTest
- AElf.Automation.ContractsTesting
- AElf.Automation.EconomicSystem.Tests
- AElf.Automation.RpcPerformance
- AElf.Automation.ScenariosExecution
- AElf.Automation.SetTransactionFees
- AElf.Automation.SideChainVerification

## How to run

### AElfChain.Console
AElfChain.Console provide lots of convenient commands to help you check node status and transaction execution.

## Feature

 01. [``chain``]-Query block chain api
 02. [``cross-chain-tx``]-Cross chain transactions
 03. [``analyze``]-Analyze block chain blocks and transactions
 04. [``call``]-Call contract view methods
 05. [``send``]-Execute contract action methods
 06. [``system-contracts``]-Query all system contracts
 07. [``token-balance``]-Query token balance info
 08. [``proposal``]-Query Proposal info by Id
 09. [``consensus``]-Query current miners information
 10. [``deploy``]-Deploy contract with authority permission
 11. [``update``]-Update contract with authority permission
 12. [``token-transfer``]-Transfer token to tester
 13. [``resource``]-Resource buy and sell
 14. [``connector``]-Set token connector
 15. [``tx-fee``]-Get/Set transaction method fee
 16. [``tx-limit``]-Get/Set transaction execution limit

### AElf.Automation.RpcPerformance
Performance testing, you can run huge transactions to test node stability and transaction execution tps.
Following are details about running step and how to configuration.

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
      "account": "G6eX2WYjeUXptQXs24QDSMiRHTMFG23PLKVFhLU1zek6SYjut",
      "password": "123"
    }
  ],
  "NativeTokenSymbol" : "ELF",
  "DefaultPassword": "123"
}
```
 
3.Set ``rpc-performance.json`` to set transaction limit and testing mode.
```
{
    "GroupCount": 4,
    "TransactionCount": 30,
    "EnableRandomTransaction": true,
    "ServiceUrl": "192.168.197.43:8100",
    "SentTxLimit": 100,
    "ExecuteMode": 4,
    "Timeout": 300,
    "RandomSenderTransaction": true,
    "NodeTransactionLimit": {
        "enable_limit": true,
        "max_transactions_select": 20
    },
    "RequestRandomEndpoint": {
        "enable_random": true,
        "endpoint_list": [
            "192.168.197.43:8100",
            "192.168.197.15:8100",
            "192.168.197.52:8100"
        ]
    }
}

```
*Adpot GroupCount and TransactionCount number can control transaction sent number frequency.*      
*GroupCount*: how many threads to sent transaction.   
*TransactionCount*: how many transactions sent each time in one thread.
*EnableRandomTransaction*: set whether sent transaction with random number from arrange (1, TransactionCount).  
*ServiceUrl*: node web api address and port to start execution.      
*SentTxLimit*: if transaction hub have more than specified txs, test will wait and not send txs.   
*RandomSenderTransaction*: sent transaction with sender are random, lot of transaction groups with set this value as true.
*NodeTransactionLimit*: set node select transaction number in each block execution.
*RequestRandomEndpoint*: set whether sent request to other endpoints.

#### Usage:
```
dotnet AElf.Automation.RpcPerformance.dll //nodes.json is default value and can be ignored
dotnet AElf.Automation.RpcPerformance.dll -c other-nodes.json
```

### AElf.Automation.ScenarioExecution
Scenario testing, test covered a lot of scenarios about contracts execution. Detail scenarios included please refer [document](https://github.com/AElfProject/aelf-automation-test/blob/dev/test/AElf.Automation.ScenariosExecution/ReadMe.md) introduction. 
1. Prepare nodes account files and copied to test script execution path ``./aelf/keys``.
2. Modify scenario-nodes config  
According test requirement, you can modify each test scenario interval and disable those scenarios you didn't want to run.
3. Run command to start
```
dotnet AElf.Automation.ScenarioExecution.dll
dotnet AElf.Automation.ScenarioExecution.dll -c nodes.json
```
#### Note:
Due to long time running and user balance verification, all scenario test users are specified and would not used for other test. So for this test all testers are prepared and other accounts exclude node accounts and tester accounts, others are deleted automatically when propgram started.
And you also no need to prepare test contracts, they are also prepared.

### AElf.Automation.SetTransactionFees
Set transaction fees for all system contract. This script will go through all system contract methods and set transaction method fee via authority. Except following methods not set due to design:
```
1. InitialAElfConsensusContract
2. FirstRound
3. NextRound
4. AEDPoSContractStub.NextTerm
5. UpdateValue
6. UpdateTinyBlockInformation
7. ClaimTransactionFees
8. DonateResourceToken
9. RecordCrossChainData
10. ChargeTransactionFees
11. CheckThreshold
12. CheckResourceToken
13. ChargeResourceToken
```
Commands:
```
dotnet AElf.Automation.SetTransactionFees -c nodes.json -e 127.0.0.1:8000 -a 50000000
-c nodes config info
-e endpoint connected to
-a transaction fee amount
```
