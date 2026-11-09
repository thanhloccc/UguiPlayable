using System;
using NUnit.Framework;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Core;

public sealed class CounterFormatMathTests
{
    [TestCase(0d, "0")]
    [TestCase(7d, "7")]
    [TestCase(999d, "999")]
    [TestCase(1000d, "1,000")]
    [TestCase(1234567d, "1,234,567")]
    [TestCase(-1234567d, "-1,234,567")]
    public void Grouping_GroupsByThree(double value, string expected)
    {
        Assert.AreEqual(expected, Format(value, new CounterChannel(0d, 0d, 0, true)));
    }

    [Test]
    public void GroupingOff_WritesPlainDigits()
    {
        Assert.AreEqual("1234567", Format(1234567d, new CounterChannel(0d, 0d, 0, false)));
    }

    [TestCase(0.125d, 2, "0.13")]
    [TestCase(0.124d, 2, "0.12")]
    [TestCase(2.5d, 0, "3")]
    [TestCase(-2.5d, 0, "-3")]
    [TestCase(0.5d, 0, "1")]
    [TestCase(-0.5d, 0, "-1")]
    [TestCase(3d, 3, "3.000")]
    [TestCase(0.05d, 1, "0.1")]
    [TestCase(0.000001d, 6, "0.000001")]
    [TestCase(-0.25d, 1, "-0.3")]
    [TestCase(12.5d, 9, "12.500000")]
    public void Decimals_RoundHalfAwayFromZero(double value, int decimals, string expected)
    {
        Assert.AreEqual(expected, Format(value, new CounterChannel(0d, 0d, decimals, false)));
    }

    [TestCase(-0d)]
    [TestCase(-0.4d)]
    [TestCase(double.NaN)]
    public void NegativeZeroAndNaN_PrintZero(double value)
    {
        Assert.AreEqual("0", Format(value, new CounterChannel(0d, 0d, 0, false)));
    }

    [Test]
    public void NegativeZeroWithDecimals_PrintsNoSign()
    {
        Assert.AreEqual("0.00", Format(-0.001d, new CounterChannel(0d, 0d, 2, false)));
    }

    [Test]
    public void PrefixAndSuffix_WrapTheNumber_SignFirst()
    {
        var format = new CounterChannel(0d, 0d, 1, true, prefix: "$", suffix: " pts");

        Assert.AreEqual("$1,234.5 pts", Format(1234.5d, format));
        Assert.AreEqual("-$1,234.5 pts", Format(-1234.5d, format));
    }

    [Test]
    public void CustomSeparators_AreUsed()
    {
        var format = new CounterChannel(0d, 0d, 2, true, groupSeparator: ".", decimalSeparator: ",");

        Assert.AreEqual("1.234.567,89", Format(1234567.89d, format));
    }

    [Test]
    public void MultiCharacterSeparators_AreUsed()
    {
        var format = new CounterChannel(0d, 0d, 1, true, groupSeparator: " ' ", decimalSeparator: "::");

        Assert.AreEqual("1 ' 000::5", Format(1000.5d, format));
    }

    [Test]
    public void NullAndEmptySeparators_ReadAsTheDefaults()
    {
        var format = new CounterChannel(0d, 0d, 1, true, groupSeparator: null, decimalSeparator: string.Empty);

        Assert.AreEqual("1,000.5", Format(1000.5d, format));
    }

    [Test]
    public void LargeValues_StayExact()
    {
        Assert.AreEqual("1,234,567,890,123", Format(1234567890123d, new CounterChannel(0d, 0d, 0, true)));
    }

    [Test]
    public void LongLimits_AreClamped()
    {
        var format = new CounterChannel(0d, 0d, 0, false);

        Assert.AreEqual("9223372036854775807", Format(1e30d, format));
        Assert.AreEqual("-9223372036854775808", Format(-1e30d, format));
        Assert.AreEqual("9223372036854775807", Format(double.PositiveInfinity, format));
        Assert.AreEqual("-9223372036854775808", Format(double.NegativeInfinity, format));
    }

    [Test]
    public void LongLimits_WithDecimalsAndGrouping_Fit()
    {
        var format = new CounterChannel(0d, 0d, 6, true, prefix: "<", suffix: ">");

        Assert.AreEqual("-9,223,372,036,854.775808".Insert(1, "<") + ">", Format(-1e30d, format));
    }

    [Test]
    public void DecimalsAbove6_AreClamped()
    {
        var format = new CounterChannel(0d, 0d, 9, false);

        Assert.AreEqual(6, format.Decimals);
        Assert.AreEqual("1.500000", Format(1.5d, format));
    }

    [TestCase(0, false)]
    [TestCase(6, true)]
    public void RequiredLength_Fits(int decimals, bool grouping)
    {
        var format = new CounterChannel(0d, 0d, decimals, grouping, "---", "<>", "PRE", "SUF");
        var buffer = new char[CounterFormatMath.RequiredLength(format)];
        var values = new[] { 0d, -1d, 1e30d, -1e30d, 123456789.123456d, -0.000001d };

        foreach (var value in values)
        {
            Assert.LessOrEqual(CounterFormatMath.Fill(buffer, value, format), buffer.Length, value.ToString("R"));
        }
    }

    [Test]
    public void DoesNotAllocate()
    {
        var format = new CounterChannel(0d, 0d, 2, true, prefix: "$", suffix: "!");
        var buffer = new char[CounterFormatMath.RequiredLength(format)];
        var sum = 0;

        for (var i = 0; i < 10; i++)
        {
            sum += CounterFormatMath.Fill(buffer, i * 1234.567d, format);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1000; i++)
        {
            sum += CounterFormatMath.Fill(buffer, i * 98765.4321d - 5000d, format);
        }

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        Assert.Greater(sum, 0);
    }

    private static string Format(double value, in CounterChannel format)
    {
        var buffer = new char[CounterFormatMath.RequiredLength(format)];
        var length = CounterFormatMath.Fill(buffer, value, format);

        return new string(buffer, 0, length);
    }
}
