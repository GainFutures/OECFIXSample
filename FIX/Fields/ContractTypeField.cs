﻿using QuickFix.Fields;

namespace OEC.FIX.Sample.FIX.Fields
{
    public class ContractTypeField : IntField
    {
        public const int ELECTRONIC = 0;
        public const int PIT = 1;
        public const int ROUTED = 2;
        public const int SIDE_BY_SIDE = 3;

        public ContractTypeField(int value)
            : base(Tags.ContractTypeField, value)
        {
        }

        public ContractTypeField()
            : this(ELECTRONIC)
        {
        }
    }
}