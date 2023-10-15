using System.Text;

namespace IFPC
{
    public class Processer
    {
        public Dictionary<string, VariableData> Global { get; internal set; }
        public Stack<Dictionary<string, VariableData>> Local { get; internal set; } = new();
        public Dictionary<string, int> Lables = new();

        public int RunningPoint { get; internal set; } = 0;

        public string[] codes = Array.Empty<string>();
        public bool IsRunning { get; internal set; } = false;

       
        public dynamic? Run(string[] codes)
        {
            this.codes = codes;
            IsRunning = true;
            RunningPoint = 0;
            return RunLoop();
        }

        internal dynamic? RunLoop()
        {
            for (int end = codes.Length; RunningPoint<end;RunningPoint++)
            {
                //문자열 다듬기
                string cmd = codes[RunningPoint];
                cmd = cmd.Trim();
                if (cmd.Length == 0 || cmd[0] == ';') return null;
                List<Token> ts = Tokenizer(cmd);

                //특수 키워드
                if (ts[0].type == TokenType.Keyword)
                {
                    switch ((Keywords)ts[0].value)
                    {
                        case Keywords.Return:
                            ts.RemoveAt(0);
                            return TokenToData(ts);
                    }
                }

                //일반적인 명령 처리
                TokenProcess(ts);
            }
            return null;
        }

        public dynamic? Query(string cmd)
        {
            cmd = cmd.Trim();
            if (cmd.Length == 0 || cmd[0] == ';') return null;
            List<Token> ts = Tokenizer(cmd);
            if (ts[0].type == TokenType.Keyword) throw new IFPCError("키워드는 대화형에서 사용할수 없습니다.");
            return TokenProcess(ts);
        }
        
        internal dynamic? TokenProcess(List<Token> ts)
        {
            //없는 변수 생성
            if (ts[0].type == TokenType.Unknown)
            {
                if (ts.Count >= 2 && ts[1].type == TokenType.Substitution)
                {
                    string name = ts[0].value!;
                    //if (!this.AddVariable(name, ts[2].value, false)) throw new IFPCError("이미 선언된 함수명 또는 변수명 입니다.");
                    ts.RemoveRange(0, 2);
                    //함수에서 값을 받는가?
                    if (ts[0].type == TokenType.Function)
                    {
                        var func = ts[0].value!;
                        ts.RemoveAt(0);
                        dynamic? input;
                        input = ExecuteFunction(func, TokenToData(ts));
                        this.AddVariable(name, input, false);
                        return input;
                    }
                    //아닌가?
                    dynamic?[] inputs = TokenToData(ts);
                    if (ts.Count != 1) throw new IFPCError("변수에 인자를 2개 이상 넣을수 없습니다. 배열을 이용할려는것이 아닌지 확인해보세요.");
                    this.AddVariable(name, inputs[0], false);
                    return inputs[0];
                }
                throw new IFPCError(ts[1].type == TokenType.Substitution ? $"존재하지 않는 변수/함수명 '{ts[0].value}' 를 제거할수 없습니다." : $"'{ts[0].value}' 는 알수없는 명령어/함수/변수 입니다.");
            }

            //레이블
            if (ts[0].type == TokenType.Lable)
            {
                Lables.Remove(ts[0].value!);
                Lables.Add(ts[0].value, this.RunningPoint);
                return null;
            }

            //함수 실행
            if (ts[0].type == TokenType.Function)
            {
                var func = ts[0].value!;
                ts.RemoveAt(0);
                return ExecuteFunction(func, TokenToData(ts));
            }

            throw new IFPCError("처리할수 없는 명령어");
        }

        internal dynamic? Into(int index)
        {
            this.RunningPoint = index;
            return RunLoop();
        }
    
        internal dynamic?[] TokenToData(List<Token> tos)
        {
            //잘못된 인자 검출  및 변수 껍질 까기
            for (int i = 0; i < tos.Count; i++)
            {
                if (tos[i].type == TokenType.Function)
                {
                    throw new IFPCError("인자에는 함수가 들어갈수 없습니다.");
                }
                if (tos[i].type == TokenType.Variable)
                {
                    VariableData vd = tos[i].value!;
                    tos[i] = new(TokenType.Data, vd!.Get);
                }
                if (tos[i].type == TokenType.Unknown)
                {
                    throw new IFPCError($"'{tos[i].value}' 는 알수없는 변수/함수/명령어 입니다.");
                }
                if (tos[i].type == TokenType.Substitution)
                {
                    throw new IFPCError("인자 내에서는 대입 연산자를 사용할수 없습니다.");
                }
                if (tos[i].type == TokenType.Keyword)
                {
                    throw new IFPCError("IFPC 기본 명령어를 인자로 사용할수 없습니다.");
                }
            }

            //곱셈, 나눗셈 먼저
            for (int i =0; i < tos.Count;i++)
            {
                if (tos[i]!.type == TokenType.Prefix)
                {
                    if (i == 0 || tos[i - 1]!.type != TokenType.Data || i+1 == tos.Count || tos[i+1]!.type != TokenType.Data) throw new IFPCError("연산자 양 옆에는 연산 가능한 값이 있어야만 합니다.");
                    if (tos[i]!.value == '*')
                    {
                        tos[i] = new(TokenType.Data, tos[i - 1]!.value * tos[i + 1]!.value);
                        tos.RemoveAt(i - 1);
                        tos.RemoveAt(i);
                        i--;
                    }
                    if (tos[i]!.value == '/')
                    {
                        if (tos[i + 1]!.value is 0) throw new IFPCError("0으로 나눌수 없습니다.");
                        tos[i] = new(TokenType.Data, tos[i - 1]!.value / tos[i + 1]!.value);
                        tos.RemoveAt(i - 1);
                        tos.RemoveAt(i);
                        i--;
                    }
                }
            }

            //그후 덧셈 뺄셈
            for (int i = 0; i < tos.Count; i++)
            {
                if (tos[i]!.type == TokenType.Prefix)
                {
                    if (i == 0 || tos[i - 1]!.type != TokenType.Data || i + 1 == tos.Count || tos[i + 1]!.type != TokenType.Data) throw new IFPCError("연산자 양 옆에는 연산 가능한 값이 있어야만 합니다.");
                    if (tos[i]!.value == '+')
                    {
                        tos[i] = new(TokenType.Data, tos[i - 1]!.value + tos[i + 1]!.value);
                        tos.RemoveAt(i - 1);
                        tos.RemoveAt(i);
                        i--;
                    }
                    if (tos[i]!.value == '-')
                    {
                        tos[i] = new(TokenType.Data, tos[i - 1]!.value - tos[i + 1]!.value);
                        tos.RemoveAt(i - 1);
                        tos.RemoveAt(i);
                        i--;
                    }
                }
            }

            //인자로 모으기
            var result = new dynamic?[tos.Count];
            for(int i=0; i<tos.Count; i++)
            {
                result[i] = tos[i].value;
            }

            return result;
        }

        public dynamic? ExecuteFunction(FunctionData func, dynamic?[] param)
        {
            if (func.IsNative)
            {
                CsharpFunction target = (CsharpFunction)func;
                return target.Func(param);
            } else
            {
                IFPCFunction target = (IFPCFunction)func;
                int returnpoint = this.RunningPoint;
                var result = this.Into(target.index);
                this.RunningPoint  = returnpoint;
                return result;
            }
        }

        public bool AddVariable(string name,dynamic? value,bool local = false)
        {
            if (local)
            {
                if (Local.Count == 0) throw new IFPCError("현재 함수에 진입한 상태가 아니므로 지역 변수를 생성할수 없습니다.");
                return Local.Last().TryAdd(name, new IFPCVariable(value));
            }
            return Global.TryAdd(name, new IFPCVariable(value));
        }

        public bool AddVariable(string name,CsharpVariable.GetVariable get, CsharpVariable.SetVariable set)
        {
            return Global.TryAdd(name,new CsharpVariable(get,set));
        }

        internal readonly char[] NormalPrefix = "=+-*/%!".ToCharArray();

        internal List<Token> Tokenizer(string cmd)
        {
            List<Token> tokens = new List<Token>();
            string one;

            void SplitPrefix()
            {
                int end = one.IndexOfAny(NormalPrefix);
                if (end != -1)
                {
                    cmd = one.Substring(end) + cmd;
                    one = one.Substring(0, end);
                }
            }

            while(cmd.Length != 0)
            {
                //문자열인가?
                if (cmd[0] == '"' || cmd[0] == '\'')
                {
                    tokens.Add(new(TokenType.Data,GetString(cmd[0],ref cmd)));
                    cmd = cmd.TrimStart();
                    continue;
                }


                int space = cmd.IndexOf(' ');
                if (space is -1)
                {
                    one = cmd;
                    cmd = string.Empty;
                }
                else
                {
                    one = cmd.Substring(0, space);
                    cmd = cmd.Substring(space+1).TrimStart();
                }

                //숫자?
                if (one[0] > 47 && one[0] < 58)
                {
                    //일단 다듬기
                    SplitPrefix();
                    if (int.TryParse(one, out space))
                    {
                        tokens.Add(new(TokenType.Data, space));
                        continue;
                    }
                    if (float.TryParse(one, out var f))
                    {
                        tokens.Add(new(TokenType.Data, f));
                        continue;
                    }
                    if (double.TryParse(one,out var d))
                    {
                        tokens.Add(new(TokenType.Data, d));
                        continue;
                    }
                    throw new IFPCError("변수명 또는 함수명의 첫글자는 숫자가 아니여야 합니다.");
                }

                //상수?
                if (one[0] == '=' && tokens.Count == 0)
                {
                    one = one.Substring(1);
                    tokens.Add(new(TokenType.ConstVariableName, one));
                    continue;
                }

                space = tokens.Count;
                //연산자?
                switch (one[0])
                {
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '%':
                    case '@':
                        tokens.Add(new(TokenType.Prefix, one[0]));
                        //cmd = cmd.Substring(1);
                        break;
                    case '=':
                        if (one.Length > 1 && one[1] == '=') space = -614;
                        tokens.Add(new(space == -614 ? TokenType.Compare : TokenType.Substitution, '='));
                        break;
                }
                if (space != tokens.Count)
                {
                    cmd = one.Substring(space == -614 ? 2 : 1) + cmd;
                    continue;
                }

                //문자 다시 다듬기
                SplitPrefix();

                //지역변수?
                if (one[0] == '#')
                {
                    if (this.Local.Count == 0) throw new IFPCError("현재 함수에 진입하지 않아 지역변수를 생성할수 없습니다.");
                    one = one.Substring(1);
                    if (Local.Last().TryGetValue(one, out var d))
                    {
                        tokens.Add(new(TokenType.Variable, d));
                        continue;
                    }
                    tokens.Add(new(TokenType.LocalVariableName, one));
                    continue;
                }

                //레이블?
                if (one[0] == ':')
                {
                    one = one.Substring(1);
                    if (one.Length == 0) throw new IFPCError("레이블에 이름을 지정하지 않았습니다.");
                    tokens.Add(new(TokenType.Lable, one));
                    continue;
                }

                //키워드?
                switch (one)
                {
                    case "return":
                        tokens.Add(new(TokenType.Keyword, Keywords.Return));
                        break;
                    case "if":
                        tokens.Add(new(TokenType.Keyword, Keywords.If));
                        break;
                    case "while":
                        tokens.Add(new(TokenType.Keyword, Keywords.While));
                        break;
                    case "goto":
                        tokens.Add(new(TokenType.Keyword, Keywords.Goto));
                        break;
                    case "jump":
                        tokens.Add(new(TokenType.Keyword, Keywords.Jump));
                        break;
                    case "exit":
                        tokens.Add(new(TokenType.Keyword, Keywords.Exit));
                        break;
                }
                if (space != tokens.Count) continue;

                bool exist = false;
                foreach (var d in Local)
                {
                    if(d.TryGetValue(one, out var e))
                    {
                        tokens.Add(new(TokenType.Variable, e));
                        exist = true;
                        break;
                    }
                }
                if (exist) continue;
                if (Global.TryGetValue(one, out var v))
                {
                    var func = v.Get;
                    exist = func is FunctionData;
                    tokens.Add(new(exist ? TokenType.Function : TokenType.Variable,exist?func:v));
                    continue;
                }

                tokens.Add(new(TokenType.Unknown, one));
            }

            return tokens;
        }

        internal string GetString(char prefix,ref string str)
        {
            StringBuilder text = new StringBuilder();
            for (int i = 1;i<str.Length;i++)
            {
                if (str[i] == prefix) {
                    str = str.Substring(i+1);
                    return text.ToString();
                }

                if (str[i] == '\\')
                {
                    if (++i == str.Length)
                    {
                        throw new IFPCError("문자열의 끝은 항상 따옴표여야 합니다.");
                    }
                    text.Append(str[i] == 'n' ? '\n' : str[i]);
                    continue;
                }

                text.Append(str[i]);
            }
            throw new IFPCError("문자열의 끝을 지정하는 따옴표가 존재하지 않습니다.");
        }

        public Processer(Commands.RequireOption option = Commands.RequireOption.All)
        {
            this.Global = Commands.GetMap(option);
        }
    }

    public class Token
    {
        public TokenType type;
        public dynamic? value;

        public Token(TokenType type, dynamic value)
        {
            this.type = type;
            this.value = value;
        }
    }

    public enum TokenType
    {
        Unknown,
        Variable,
        Function,
        Prefix,
        Data,
        LocalVariableName,
        ConstVariableName,
        Compare,
        Substitution,
        Lable,
        Keyword
    }

    public enum Keywords
    {
        Return,
        If,
        While,
        Exit,
        Goto,
        Jump
    }

    public abstract class VariableData
    {
        public abstract bool IsNative { get; }
        public bool IsConstant { get; internal set; }

        public abstract dynamic? Get { get; }
        public abstract dynamic? Set { set; }
    }

    public class IFPCVariable : VariableData
    {
        public override bool IsNative => false;

        internal dynamic? Data;
        public override dynamic? Get => Data;
        public override dynamic? Set { set => this.Data = value; }

        public IFPCVariable(dynamic? data = null)
        {
            this.Data = data;
        }
    }

    public class CsharpVariable : VariableData
    {
        public delegate dynamic? GetVariable();
        public delegate void SetVariable(dynamic? value);

        public override bool IsNative => true;

        GetVariable getaction;
        SetVariable setaction;

        public override dynamic? Get => getaction;
        public override dynamic? Set { set => setaction(value); }

        public CsharpVariable(GetVariable get, SetVariable set)
        {
            this.getaction = get;
            this.setaction = set;
        }
    }

    public abstract class FunctionData
    {
        public abstract bool IsNative { get; }
    }

    public class IFPCFunction : FunctionData
    {
        public override bool IsNative => false;

        public int index { get; }

        public IFPCFunction(int index)
        {
            this.index = index;
        }
    }

    public class CsharpFunction : FunctionData
    {
        public delegate dynamic? NativeFunction(dynamic?[] data);

        public override bool IsNative => true;
        public NativeFunction Func { get; set; }

        public CsharpFunction(NativeFunction f)
        {
            this.Func = f;
        }
    }

    public class VariableMap : Dictionary<string, VariableData>
    {
        public void Add(string key,dynamic? data)
        {
            base.Add(key,new IFPCVariable(data));
        }
    }

    public class VariableInfo
    {
        public string Name { get; set; }
        public VariableData Value { get; set; }

        public VariableInfo(string name,VariableData value)
        {
            this.Name = name;
            this.Value = value;
        }

        public VariableInfo(string name,CsharpFunction.NativeFunction func)
        {
            this.Name = name;
            this.Value = new IFPCVariable(new CsharpFunction(func));
        }

        public VariableInfo(string name,CsharpVariable.GetVariable get, CsharpVariable.SetVariable set)
        {
            this.Name = name;
            this.Value = new CsharpVariable(get, set);
        }
    }

    public static class Commands
    {
        public static VariableInfo[] Console = new VariableInfo[]
        {
            new("console.write",x => {
                if (x.Length == 0) throw new IFPCError("인자가 없습니다.");
                if (x.Length == 1) System.Console.Write(x[0]);
                else
                {
                    if (x[0] is not string) throw new IFPCError("여러 인자를 받아 출력하는 경우, 첫번째 인자는 문자열이여야만 합니다.");
                    string message = x[0]!; x[0] = string.Empty;
                    System.Console.Write(message, x);
                }
                return null;
            }),
            new("console.read",x =>
            {
                return System.Console.ReadLine();
            }),
            new("console.clear", x =>
            {
                System.Console.Clear();
                return null;
            }),
        };
        public static VariableInfo[] Convert = new VariableInfo[]
        {
            new("convert.int",x =>
            {
                if (x.Length == 0) throw new IFPCError("변환할 값을 넣어주세요.");
                if (int.TryParse(x[0], out int value))
                {
                    return value;
                }
                return null;
            }),
            new("convert.string",x =>
            {
                if (x.Length == 0) return string.Empty;
                return new string(x[0]);
            }),
            new("convert.float",x =>
            {
                if (x.Length == 0) throw new IFPCError("변환할 값을 넣어주세요.");
                if (float.TryParse(x[0],out float value))
                {
                    return value;
                }
                return null;
            })
        };

        public static Dictionary<string,VariableData> GetMap(RequireOption option = RequireOption.All)
        {
            Dictionary<string, VariableData> result = new();
            void Inject(Enum flag, VariableInfo[] target)
            {
                if (option.HasFlag(flag))
                {
                    foreach (var item in target)
                    {
                        result.Add(item.Name, item.Value);
                    }
                }
            }

            Inject(RequireOption.Console, Console);
            Inject(RequireOption.Convert, Convert);

            return result;
        }

        [Flags]
        public enum RequireOption
        {
            Console = 1,
            Convert = 2,

            All = 127
        }
    }

    public class IFPCError : Exception
    {
        public IFPCError() : base() { }
        public IFPCError(string message) : base(message) { }
    }
}
