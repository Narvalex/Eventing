using System.Reflection.Emit;
using System.Text;

namespace Infrastructure.Reflection
{
    public sealed class Instruction
    {
        private readonly int offset;
        private readonly OpCode opcode;
        private object operand;

        private Instruction previous;

        public int Offset => this.offset; 

        public OpCode OpCode => this.opcode; 

        public object Operand
        {
            get { return this.operand; }
            internal set { this.operand = value; }
        }

        public Instruction Previous
        {
            get { return this.previous; }
            internal set { previous = value; }
        }

        public Instruction Next { get; internal set; }

        public int Size
        {
            get
            {
                int size = opcode.Size;

                switch (opcode.OperandType)
                {
                    case OperandType.InlineSwitch:
                        size += (1 + ((Instruction[])operand).Length) * 4;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        size += 8;
                        break;
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineField:
                    case OperandType.InlineI:
                    case OperandType.InlineMethod:
                    case OperandType.InlineString:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.ShortInlineR:
                        size += 4;
                        break;
                    case OperandType.InlineVar:
                        size += 2;
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        size += 1;
                        break;
                }

                return size;
            }
        }

        internal Instruction(int offset, OpCode opcode)
        {
            this.offset = offset;
            this.opcode = opcode;
        }

        public override string ToString()
        {
            var instruction = new StringBuilder();

            AppendLabel(instruction, this);
            instruction.Append(':');
            instruction.Append(' ');
            instruction.Append(opcode.Name);

            if (operand == null)
                return instruction.ToString();

            instruction.Append(' ');

            switch (this.opcode.OperandType)
            {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    AppendLabel(instruction, (Instruction)operand);
                    break;
                case OperandType.InlineSwitch:
                    var labels = (Instruction[])operand;
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if (i > 0)
                            instruction.Append(',');

                        AppendLabel(instruction, labels[i]);
                    }
                    break;
                case OperandType.InlineString:
                    instruction.Append('\"');
                    instruction.Append(operand);
                    instruction.Append('\"');
                    break;
                default:
                    instruction.Append(operand);
                    break;
            }

            return instruction.ToString();
        }

        static void AppendLabel(StringBuilder builder, Instruction instruction)
        {
            builder.Append("IL_");
            builder.Append(instruction.offset.ToString("x4"));
        }
    }
}
