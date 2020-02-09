using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.CSharp.Analysis;
using Roslynator.CSharp.CSharp.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCS1242MakeDataContractNamesAndOrderExplicitTests : AbstractCSharpFixVerifier
    {
        private static readonly MakeDataContractNamesAndOrderExplicitCodeFixProvider _fixProvider = new MakeDataContractNamesAndOrderExplicitCodeFixProvider();

        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticDescriptors.MakeDataContractNamesAndOrderExplicit;

        public override DiagnosticAnalyzer Analyzer { get; } = new MakeDataContractNamesAndOrderExplicitAnalyzer();

        public override CodeFixProvider FixProvider { get; } = _fixProvider;

        private const string FakeAttributes = @"
            namespace System.Runtime.Serialization
            {
                [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
                internal class DataMemberAttribute : Attribute
                {
                    public int Order { get; set; }
                    public string Name { get; set; }
                    public bool IsRequired { get; set; }
                }
                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
                internal class DataContractAttribute : Attribute
                {
                    public string Namespace { get; set; }
                    public string Name { get; set; }
                    public bool IsReference { get; set; }
                }
            }
        ";

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_NoDiagnostic()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name=""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name =""P1"", Order =1)]
                    public int P1 { get; set; }

                    [DataMember(Name= ""P2"", Order= 2)]
                    public int P2 { get; set; }
                }"
                + FakeAttributes;

            await VerifyNoDiagnosticAsync(source);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_AddDataMemberNameAndOrder()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class [|C1|]
                {
                    [DataMember]
                    public int P1 { get; set; }

                    [DataMember()]
                    public int P2 { get; set; }

                    [DataMember(IsRequired = true)]
                    public int P3 { get; set; }

                    [DataMember(Name = ""P4X"", IsRequired = true)]
                    public int P4 { get; set; }
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name = ""P1"", Order = 1)]
                    public int P1 { get; set; }

                    [DataMember(Name = ""P2"", Order = 2)]
                    public int P2 { get; set; }

                    [DataMember(IsRequired = true, Name = ""P3"", Order = 3)]
                    public int P3 { get; set; }

                    [DataMember(IsRequired = true, Name = ""P4X"", Order = 4)]
                    public int P4 { get; set; }
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_AddDataContractName1()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract]
                public class [|C1|]
                {
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"")]
                public class C1
                {
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_AddDataContractName2()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract()]
                public class [|C1|]
                {
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"")]
                public class C1
                {
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_SetOrderAlpha1()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class [|C1|]
                {
                    [DataMember]
                    public int P1 { get; set; }

                    [DataMember]
                    public int P2 { get; set; }

                    [DataMember]
                    public int P3 { get; set; }
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name = ""P1"", Order = 1)]
                    public int P1 { get; set; }

                    [DataMember(Name = ""P2"", Order = 2)]
                    public int P2 { get; set; }

                    [DataMember(Name = ""P3"", Order = 3)]
                    public int P3 { get; set; }
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_SetOrderAlpha2()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class [|C1|]
                {
                    [DataMember]
                    public int p1 { get; set; }

                    [DataMember]
                    public int p2 { get; set; }

                    [DataMember]
                    public int P3 { get; set; }
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name = ""p1"", Order = 2)]
                    public int p1 { get; set; }

                    [DataMember(Name = ""p2"", Order = 3)]
                    public int p2 { get; set; }

                    [DataMember(Name = ""P3"", Order = 1)]
                    public int P3 { get; set; }
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_SetOrderHonourExistingOrder()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class [|C1|]
                {
                    [DataMember(Order = 3)]
                    public int P1 { get; set; }

                    [DataMember]
                    public int P2 { get; set; }

                    [DataMember(Order = 5)]
                    public int P3 { get; set; }
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name = ""P1"", Order = 2)]
                    public int P1 { get; set; }

                    [DataMember(Name = ""P2"", Order = 1)]
                    public int P2 { get; set; }

                    [DataMember(Name = ""P3"", Order = 3)]
                    public int P3 { get; set; }
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_SetOrderHonourExistingOrderAndAlpha()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class [|C1|]
                {
                    [DataMember(Order = 3)]
                    public int Pz { get; set; }

                    [DataMember]
                    public int P2 { get; set; }

                    [DataMember(Order = 3)]
                    public int Pa { get; set; }
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name = ""Pz"", Order = 3)]
                    public int Pz { get; set; }

                    [DataMember(Name = ""P2"", Order = 1)]
                    public int P2 { get; set; }

                    [DataMember(Name = ""Pa"", Order = 2)]
                    public int Pa { get; set; }
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_SetOrderHonourExistingNameAlpha()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class [|C1|]
                {
                    [DataMember(Name = ""C"")]
                    public int A { get; set; }

                    [DataMember(Name = ""A"")]
                    public int B { get; set; }

                    [DataMember(Name = ""B"")]
                    public int C { get; set; }
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name = ""C"", Order = 3)]
                    public int A { get; set; }

                    [DataMember(Name = ""A"", Order = 1)]
                    public int B { get; set; }

                    [DataMember(Name = ""B"", Order = 2)]
                    public int C { get; set; }
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_SetOrderHonourExistingNameAndOrder()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class [|C1|]
                {
                    [DataMember(Name = ""C"", Order = 1)]
                    public int A { get; set; }

                    [DataMember(Name = ""A"", Order = 1)]
                    public int B { get; set; }

                    [DataMember(Name = ""B"")]
                    public int C { get; set; }
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                public class C1
                {
                    [DataMember(Name = ""C"", Order = 3)]
                    public int A { get; set; }

                    [DataMember(Name = ""A"", Order = 2)]
                    public int B { get; set; }

                    [DataMember(Name = ""B"", Order = 1)]
                    public int C { get; set; }
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.MakeDataContractNamesAndOrderExplicit)]
        public async Task Test_StructAndField()
        {
            const string source =
                @"
                using System.Runtime.Serialization;
                [DataContract(Namespace = ""n1"")]
                struct [|C1|]
                {
                    [DataMember(Name = ""C"", Order = 1)]
                    private int A;

                    [DataMember(Name = ""A"", Order = 1)]
                    public int B { get; set; }

                    [DataMember(Name = ""B"")]
                    public int C;
                }"
                + FakeAttributes;

            const string expected =
                @"
                using System.Runtime.Serialization;
                [DataContract(Name = ""C1"", Namespace = ""n1"")]
                struct C1
                {
                    [DataMember(Name = ""C"", Order = 3)]
                    private int A;

                    [DataMember(Name = ""A"", Order = 2)]
                    public int B { get; set; }

                    [DataMember(Name = ""B"", Order = 1)]
                    public int C;
                }"
                + FakeAttributes;

            await VerifyDiagnosticAndFixAsync(source, expected);
        }
    }
}
