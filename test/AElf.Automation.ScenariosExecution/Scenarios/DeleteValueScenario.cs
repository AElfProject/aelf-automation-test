using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElfChain.Common.Contracts;
using Shouldly;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class DeleteValueScenario : BaseScenario
    {
        private const int IncreaseActionCount = 10;
        private readonly List<BasicWithParallelContract> _contracts;

        public DeleteValueScenario()
        {
            InitializeScenario();
            Testers = AllTesters.GetRange(5, 5);
            PrintTesters(nameof(DeleteValueScenario), Testers);

            var contract =
                BasicWithParallelContract.GetOrDeployBasicWithParallelContract(Services.NodeManager, Testers[0]);
            _contracts = Testers.Select(t =>
                new BasicWithParallelContract(Services.NodeManager, t, contract.ContractAddress)).ToList();
        }

        public List<string> Testers { get; }

        public void RunDeleteValueScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                DeleteValueAction,
                IncreaseValueAction,
                DeleteValueParallelAction,
                IncreaseValueParallelAction,
                DeleteValueAfterSetAction,
                SetValueAfterDeleteAction,
                ComplexDeleteAndChangeAction,
                () => PrepareTesterToken(Testers),
                UpdateEndpointAction
            });
        }

        private void IncreaseValueAction()
        {
            foreach (var contract in _contracts)
                for (var i = 0; i < IncreaseActionCount; i++)
                    contract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseValue, new IncreaseValueInput
                    {
                        Key = contract.CallAddress,
                        Memo = Guid.NewGuid().ToString()
                    });
            _contracts.ForEach(c => c.CheckTransactionResultList());
            CheckValue(IncreaseActionCount);
        }

        private void IncreaseValueParallelAction()
        {
            foreach (var contract in _contracts)
                for (var i = 0; i < IncreaseActionCount; i++)
                    if (i < IncreaseActionCount / 2)
                        contract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseValueParallel, new IncreaseValueInput
                        {
                            Key = contract.CallAddress,
                            Memo = Guid.NewGuid().ToString()
                        });
                    else
                        contract.ExecuteMethodWithTxId(BasicParallelMethod.IncreaseValue, new IncreaseValueInput
                        {
                            Key = contract.CallAddress,
                            Memo = Guid.NewGuid().ToString()
                        });
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

        private void CheckValue(string value)
        {
            foreach (var contract in _contracts)
            {
                var output = contract.CallViewMethod<GetValueOutput>(BasicParallelMethod.GetValue, new GetValueInput
                {
                    Key = contract.CallAddress
                });
                output.StringValue.ShouldBe(value);
            }
        }

        private void CheckValue(MessageValue value)
        {
            foreach (var contract in _contracts)
            {
                var output = contract.CallViewMethod<GetValueOutput>(BasicParallelMethod.GetValue, new GetValueInput
                {
                    Key = contract.CallAddress
                });
                output.MessageValue.ShouldBe(value);
            }
        }

        private void DeleteValueAction()
        {
            foreach (var contract in _contracts)
                contract.ExecuteMethodWithTxId(BasicParallelMethod.RemoveValue, new RemoveValueInput
                {
                    Key = contract.CallAddress,
                    Memo = Guid.NewGuid().ToString()
                });

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(0);
        }

        private void DeleteValueParallelAction()
        {
            foreach (var contract in _contracts)
                contract.ExecuteMethodWithTxId(BasicParallelMethod.RemoveValueParallel, new RemoveValueInput
                {
                    Key = contract.CallAddress,
                    Memo = Guid.NewGuid().ToString()
                });
            foreach (var contract in _contracts)
                contract.ExecuteMethodWithTxId(BasicParallelMethod.RemoveValue, new RemoveValueInput
                {
                    Key = contract.CallAddress,
                    Memo = Guid.NewGuid().ToString()
                });

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(0);
        }

        private void DeleteValueAfterSetAction()
        {
            foreach (var contract in _contracts)
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

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(0);
        }

        private void SetValueAfterDeleteAction()
        {
            foreach (var contract in _contracts)
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

            _contracts.ForEach(c => c.CheckTransactionResultList());

            CheckValue(10);
        }

        private void ComplexDeleteAndChangeAction()
        {
            //test ComplexChangeWithDeleteValue1
            foreach (var contract in _contracts)
                contract.ExecuteMethodWithTxId(BasicParallelMethod.ComplexChangeWithDeleteValue1, new ComplexChangeInput
                {
                    Key = contract.CallAddress,
                    Int64Value = 100,
                    StringValue = "new-info1",
                    MessageValue = new MessageValue
                    {
                        StringValue = Guid.NewGuid().ToString()
                    }
                });

            _contracts.ForEach(c => c.CheckTransactionResultList());
            CheckValue(100);
            CheckValue("new-info1");
            CheckValue((MessageValue) null);

            //test ComplexChangeWithDeleteValue2
            var message = new MessageValue
            {
                Int64Value = 200,
                StringValue = "test2"
            };
            foreach (var contract in _contracts)
                contract.ExecuteMethodWithTxId(BasicParallelMethod.ComplexChangeWithDeleteValue2, new ComplexChangeInput
                {
                    Key = contract.CallAddress,
                    Int64Value = Guid.NewGuid().GetHashCode(),
                    StringValue = "new-info2",
                    MessageValue = message
                });

            _contracts.ForEach(c => c.CheckTransactionResultList());
            CheckValue(0);
            CheckValue("new-info2");
            CheckValue(message);

            //test ComplexChangeWithDeleteValue3
            foreach (var contract in _contracts)
                contract.ExecuteMethodWithTxId(BasicParallelMethod.ComplexChangeWithDeleteValue3, new ComplexChangeInput
                {
                    Key = contract.CallAddress,
                    Int64Value = 300,
                    StringValue = Guid.NewGuid().ToString()
                });

            _contracts.ForEach(c => c.CheckTransactionResultList());
            CheckValue(300);
            CheckValue("");
            CheckValue((MessageValue) null);
        }
    }
}