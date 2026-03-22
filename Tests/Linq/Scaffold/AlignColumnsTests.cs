using System.Collections.Generic;
using System.Linq;

using LinqToDB.CodeModel;

using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public class AlignColumnsTests : TestBase
	{
		/// <summary>
		/// Generates source code for a class with two properties of different type/name lengths,
		/// using the specified <paramref name="alignColumns"/> setting.
		/// </summary>
		private static string GenerateProperties(bool alignColumns)
		{
			var lang    = LanguageProviders.CSharp;
			var builder = lang.ASTBuilder;

			// build a minimal code file: namespace → class → two properties with tableLayout: true
			var file = builder.File("Test");
			var ns   = builder.Namespace("TestNamespace");
			file.Add(ns.Namespace);

			var classGroup = ns.Classes();
			var cls        = classGroup.New(new CodeIdentifier("TestEntity", true));
			cls.SetModifiers(Modifiers.Public);

			var props = cls.Properties(tableLayout: true);

			// short type + long name vs long type + short name → alignment padding is visible
			props.New(new CodeIdentifier("Id", true), WellKnownTypes.System.Int32)
				.SetModifiers(Modifiers.Public)
				.Default(true);
			props.New(new CodeIdentifier("LongPropertyName", true), WellKnownTypes.System.String)
				.SetModifiers(Modifiers.Public)
				.Default(true);

			// generate source code
			var emptyDict1 = new Dictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>>();
			var emptyDict2 = new Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>();

			var codeGenerator = lang.GetCodeGenerator(
				"\n",
				"\t",
				useNRT: false,
				alignColumns: alignColumns,
				emptyDict1,
				emptyDict2,
				emptyDict2);

			codeGenerator.Visit(file);
			return codeGenerator.GetResult();
		}

		[Test]
		public void AlignedOutput_ContainsPaddedColumns()
		{
			var result = GenerateProperties(alignColumns: true);

			// With alignment enabled, the shorter type "int" is padded to match "string" length,
			// and property names are also padded, producing extra spaces.
			var lines = result.Split('\n')
				.Select(l => l.TrimEnd('\r'))
				.Where(l => l.Contains("{ get;"))
				.ToArray();

			Assert.That(lines, Has.Length.EqualTo(2), "Expected 2 property lines");

			// In table layout, the "int" line should be padded to align with "string":
			// "public int    Id               { get; set; }"
			// "public string LongPropertyName { get; set; }"
			// The int line should have extra spaces after "int" to match "string" width.
			var intLine    = lines.First(l => l.Contains("Id"));
			var stringLine = lines.First(l => l.Contains("LongPropertyName"));

			// Both lines should have the same length up to the property name column end
			// because table layout pads columns. We just verify the int line has extra spaces
			// between "int" and "Id" compared to a minimal "int Id" representation.
			Assert.That(intLine, Does.Match(@"int\s{2,}"), "int should be padded with extra spaces in aligned mode");
		}

		[Test]
		public void UnalignedOutput_HasNoPadding()
		{
			var result = GenerateProperties(alignColumns: false);

			var lines = result.Split('\n')
				.Select(l => l.TrimEnd('\r'))
				.Where(l => l.Contains("{ get;"))
				.ToArray();

			Assert.That(lines, Has.Length.EqualTo(2), "Expected 2 property lines");

			var intLine = lines.First(l => l.Contains("Id"));

			// Without alignment, "int" is immediately followed by a single space then "Id":
			// "public int Id { get; set; }"
			Assert.That(intLine, Does.Contain("int Id"), "int should be followed by single space + Id in unaligned mode");
			Assert.That(intLine, Does.Not.Match(@"int\s{2,}Id"), "int should not have extra padding before Id in unaligned mode");
		}

		[Test]
		public void AlignedAndUnaligned_ProduceDifferentOutput()
		{
			var aligned   = GenerateProperties(alignColumns: true);
			var unaligned = GenerateProperties(alignColumns: false);

			Assert.That(aligned, Is.Not.EqualTo(unaligned), "Aligned and unaligned output should differ when properties have varying type/name lengths");
		}
	}
}
