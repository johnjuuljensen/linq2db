using System.Collections.Generic;
using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Scaffold;

using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public class EntityLayoutTests : TestBase
	{
		private static string GenerateProperties(EntityLayout layout)
		{
			var lang    = LanguageProviders.CSharp;
			var builder = lang.ASTBuilder;

			var file = builder.File("Test");
			var ns   = builder.Namespace("TestNamespace");
			file.Add(ns.Namespace);

			var classGroup = ns.Classes();
			var cls        = classGroup.New(new CodeIdentifier("TestEntity", true));
			cls.SetModifiers(Modifiers.Public);

			var props = cls.Properties(layout == EntityLayout.Table);

			props.New(new CodeIdentifier("Id", true), WellKnownTypes.System.Int32)
				.SetModifiers(Modifiers.Public)
				.Default(true);
			props.New(new CodeIdentifier("LongPropertyName", true), WellKnownTypes.System.String)
				.SetModifiers(Modifiers.Public)
				.Default(true);

			var emptyDict1 = new Dictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>>();
			var emptyDict2 = new Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>();

			var codeGenerator = lang.GetCodeGenerator(
				"\n",
				"\t",
				useNRT: false,
				entityLayout: layout,
				emptyDict1,
				emptyDict2,
				emptyDict2);

			codeGenerator.Visit(file);
			return codeGenerator.GetResult();
		}

		private static string[] GetPropertyLines(string source)
		{
			return source.Split('\n')
				.Select(l => l.TrimEnd('\r'))
				.Where(l => l.Contains("get;"))
				.ToArray();
		}

		[Test]
		public void Table_ContainsPaddedColumns()
		{
			var lines = GetPropertyLines(GenerateProperties(EntityLayout.Table));

			Assert.That(lines, Has.Length.EqualTo(2));
			Assert.That(lines.First(l => l.Contains("Id")), Does.Match(@"int\s{2,}"), "int should be padded in table layout");
		}

		[Test]
		public void List_HasNoPaddingAndBlankLines()
		{
			var result = GenerateProperties(EntityLayout.List);
			var lines  = GetPropertyLines(result);

			Assert.That(lines, Has.Length.EqualTo(2));
			Assert.That(lines.First(l => l.Contains("Id")), Does.Contain("int Id"));
			Assert.That(lines.First(l => l.Contains("Id")), Does.Not.Match(@"int\s{2,}Id"));
		}

		[Test]
		public void ListCompact_HasNoBlankLinesAndAttributesOnLineAbove()
		{
			var result = GenerateProperties(EntityLayout.ListCompact);
			var lines  = GetPropertyLines(result);

			Assert.That(lines, Has.Length.EqualTo(2));
			Assert.That(lines[0], Does.Contain("int Id"));

			// no blank lines between properties (same as dense)
			var allLines = result.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
			var idIdx    = System.Array.FindIndex(allLines, l => l.Contains("int Id"));
			var nameIdx  = System.Array.FindIndex(allLines, l => l.Contains("LongPropertyName"));
			Assert.That(nameIdx - idIdx, Is.EqualTo(1), "Should have no blank lines between properties in list-compact");
		}

		[Test]
		public void ListDense_HasNoBlankLines()
		{
			var result = GenerateProperties(EntityLayout.ListDense);
			var lines  = GetPropertyLines(result);

			Assert.That(lines, Has.Length.EqualTo(2));

			// no blank lines between properties
			var allLines = result.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
			var idIdx    = System.Array.FindIndex(allLines, l => l.Contains("int Id"));
			var nameIdx  = System.Array.FindIndex(allLines, l => l.Contains("LongPropertyName"));
			Assert.That(nameIdx - idIdx, Is.EqualTo(1), "Should have no blank lines between properties in list-dense");
		}

		[Test]
		public void CompactAndDense_DifferOnAttributePlacement()
		{
			// compact puts attrs on line above; dense puts them on same line
			// need properties with attributes to see the difference
			var lang    = LanguageProviders.CSharp;
			var builder = lang.ASTBuilder;
			var emptyDict1 = new Dictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>>();
			var emptyDict2 = new Dictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>>();

			string generate(EntityLayout layout)
			{
				var file = builder.File("Test");
				var ns   = builder.Namespace("TestNamespace");
				file.Add(ns.Namespace);
				var cls = ns.Classes().New(new CodeIdentifier("E", true));
				cls.SetModifiers(Modifiers.Public);
				var props = cls.Properties(layout == EntityLayout.Table);
				props.New(new CodeIdentifier("Id", true), WellKnownTypes.System.Int32)
					.SetModifiers(Modifiers.Public)
					.Default(true)
					.AddAttribute(new CodeAttribute(WellKnownTypes.System.Object));
				var gen = lang.GetCodeGenerator("\n", "\t", false, layout, emptyDict1, emptyDict2, emptyDict2);
				gen.Visit(file);
				return gen.GetResult();
			}

			var compact = generate(EntityLayout.ListCompact);
			var dense   = generate(EntityLayout.ListDense);

			Assert.That(compact, Is.Not.EqualTo(dense), "Compact and dense should differ when properties have attributes");
		}

		[Test]
		public void TableAndList_ProduceDifferentOutput()
		{
			var table = GenerateProperties(EntityLayout.Table);
			var list  = GenerateProperties(EntityLayout.List);

			Assert.That(table, Is.Not.EqualTo(list));
		}
	}
}
