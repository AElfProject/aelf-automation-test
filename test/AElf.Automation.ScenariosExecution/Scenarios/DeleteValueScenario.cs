using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using Shouldly;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class DeleteValueScenario : BaseScenario
    {
        private List<BasicWithParallelContract> _contracts;
        private const int IncreaseActionCount = 10;
        
        public DeleteValueScenario()
        {
            InitializeScenario();
            var contract = new BasicWithParallelContract(Services.NodeManager, AllTesters[0]);
            var testers = AllTesters.GetRange(1, 2);
            _contracts = testers.Select(t =>
                new BasicWithParallelContract(Services.NodeManager, t, contract.ContractAddress)).ToList();
        }

        public void RunDeleteValueScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                DeleteValueAction,
                IncreaseValueAction,
                DeleteValueParallelAction,
                IncreaseValueParallelAction,
                DeleteValueAfterSetAction,
                SetValueAfterDeleteAction
            });
        }

        private void IncreaseValueAction()
        {
            foreach (var contract in _contracts)
            {
                for (int i = 0; i < IncreaseActionCount; i++)
                {
                    contract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseValue,new IncreaseValueInput
                    {
                        Key = contract.CallAddress,
                        Memo = Guid.NewGuid().ToString()
                    });
                }
            }
            _contracts.ForEach(c => c.CheckTransactionResultList());
            CheckValue(IncreaseActionCount);
        }

        private void IncreaseValueParallelAction()
        {
            foreach (var contract in _contracts)
            {
                for (int i = 0; i < IncreaseActionCount; i++)
                {
                    if (i < IncreaseActionCount / 2)
                    {
                        contract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseValueParallel,new IncreaseValueInput
                        {
                            Key = contract.CallAddress,
                            Memo = Guid.NewGuid().ToString()
                        });
                    }
                    else
                    {
                        contract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseValue,new IncreaseValueInput
                        {
                            Key = contract.CallAddress,
                            Memo = Guid.NewGuid().ToString()
                        });
                    }
                }
            }
            _contracts.ForEach(c => c.CheckTransactionResultList());
            CheckValue(IncreaseActionCount);
        }

        private void CheckValue(int value)
        {
            foreach (var contract in _contracts)
            {
                var output = contract.CallViewMethod<GetValueOutput>(BasicParallelMethod.GetValue, new GetValueInput
                {
                    Key = contract.CallAddress
                });
                output.Int64Value.ShouldBe(value);
            }
        }

        private void DeleteValueAction()
        {
            foreach (var contract in _contracts)
            {
                contract.ExecuteMethodWithTxId(BasicParallelMethod.RemoveValue, new RemoveValueInput
                {
                    Key = contract.CallAddress,
                    Memo = Guid.NewGuid().ToString()
                });
            }

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(0);
        }

        private void DeleteValueParallelAction()
        {
            foreach (var contract in _contracts)
            {
                contract.ExecuteMethodWithTxId(BasicParallelMethod.RemoveValueParallel, new RemoveValueInput
                {
                    Key = contract.CallAddress,
                    Memo = Guid.NewGuid().ToString()
                });
            }
            foreach (var contract in _contracts)
            {
                contract.ExecuteMethodWithTxId(BasicParallelMethod.RemoveValue, new RemoveValueInput
                {
                    Key = contract.CallAddress,
                    Memo = Guid.NewGuid().ToString()
                });
            }

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(0);
        }

        private void DeleteValueAfterSetAction()
        {
            foreach (var contract in _contracts)
            {
                contract.ExecuteMethodWithTxId(BasicParallelMethod.RemoveAfterSetValue, new RemoveAfterSetValueInput
                {
                    Key = contract.CallAddress,
                    Int64Value = 10,
                    MessageValue = new MessageValue
                    {
                        Int64Value = 10,
                        StringValue = Guid.NewGuid().ToString()
                    }
                });
            }

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(0);
        }

        private void SetValueAfterDeleteAction()
        {
            foreach (var contract in _contracts)
            {
                contract.ExecuteMethodWithTxId(BasicParallelMethod.SetAfterRemoveValue, new SetAfterRemoveValueInput
                {
                    Key = contract.CallAddress,
                    Int64Value = 10,
                    MessageValue = new MessageValue
                    {
                        Int64Value = 10,
                        StringValue = Guid.NewGuid().ToString()
                    }
                });
            }

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(10);
        }
    }
}