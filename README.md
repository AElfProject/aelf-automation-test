# aelf-automation-test
Automation test project for AElf

## Clone and build code
aelf-automation-test is based on AElf project, so please clone code with *--recursive* command.
``` 
git clone https://github.com/AElfProject/aelf-automation-test --recursive
cd ./aelf-automation-test
git pull
cd ./src/AElf
git checkout dev
git pull
cd ./aelf-automation-test
dotnet build --configuration Release -o ./build-test-dir
```

## Test type
- AElf.Automation.Contracts.ScenarioTest
- AElf.Automation.ContractsTesting
- AElf.Automation.EconomicSystem.Tests
- AElf.Automation.QueryTransaction
- AElf.Automation.RpcPerformance
- AElf.Automation.ScenariosExecution
- AElf.Automation.SideChainVerification

## How to run

### AElf.Automation.QueryTransaction
Console program, change service url and select operation provided number and running.

### AElf.Automation.RpcPerformance
Performance testing, you can run huge transactions to test node stability and transaction execution tps.
Following are details about running step and how to configuration.

1. Prepare test contract MultiToken to default directory
```
mkdir -p ~/.local/share/aelf/contracts
cp AElf.Contracts.MultiToken.dll ~/.local/share/aelf/contracts
```

2. Run test to send transaction with configuration rpc-performance.json
```
{
    "GroupCount": 4,
    "TransactionCount": 30,
    "EnableRandomTransaction": true,
    "ServiceUrl": "192.168.197.13:8000",
    "SentTxLimit": 100,
    "ExecuteMode": 4,
    "Timeout": 300,
    "Conflict": true,
    "ReadOnlyTransaction": false,
    "NodeTransactionLimit": {
        "enable_limit": true,
        "max_transactions_select": 100
    },
    "RequestRandomEndpoint": {
        "enable_random": true,
        "endpoint_list": [
            "192.168.197.13:8000",
            "192.168.197.28:8000"
        ]
    }
}

dotnet AElf.Automation.RpcPerformance.dll
```
**Note**:   
Adpot GroupCount and TransactionCount number can control transaction sent number frequency.      
*GroupCount*: how many threads to sent transaction.   
*TransactionCount*: how many transactions sent each time in one thread.
*EnableRandomTransaction*: set whether sent transaction with random number from arrange (1, TransactionCount).  
*ServiceUrl*: node web api address and port.      
*SentTxLimit*: if transaction hub have more than specified txs, test will wait and not send txs.   
*Conflict*: default is true, at most have ThreadCount group txs. If set false, all txs with no conflict.   
*ReadOnlyTransaction*: only sent transactions with query data and not change state db.
*NodeTransactionLimit*: set node select transaction number in each round.
*RequestRandomEndpoint*: set whether sent request to other endpoints.

3. Or run test with command line
```
dotnet AElf.Automation.RpcPerformance.dll -tc 4 -tg 50 -ru http://127.0.0.1:8000 -em 4
```
**Note**:    
Both command line and configure set, command line parameter will be works.    
tc - test thread count/group      
tg - each group transaction count     
ru - node web api address      
em - test mode

### AElf.Automation.ScenarioExecution
Scenario testing, test covered a lot of scenarios about contracts execution. Detail scenarios included please refer [document](https://github.com/AElfProject/aelf-automation-test/blob/dev/test/AElf.Automation.ScenariosExecution/ReadMe.md) introduction. 
1. Prepare test contract  
Copy contracts files *AElf.Contracts.MultiToken*, *AElf.Contracts.TestContract.BasicFunction* and *AElf.Contracts.TestContract.BasicUpdate* to path ~/.local/share/aelf/contracts.
2. Prepare test node account  
Copy test account into build directory *./build-test-dir/aelf/keys*
3. Modify scenario-nodes config  
According node types to config node name, service url and accounts information.
4. Run command to start
```
dotnet AElf.Automation.ScenarioExecution.dll
```
*Note*:  
If you just want to run specified test scenario, you can disable those cases you didn't want to run.
