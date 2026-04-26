namespace Trellis.ServiceLevelIndicators.Tests;

using System;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

public class ServiceLevelIndicatorMeterProviderBuilderExtensionsTests
{
    [Fact]
    public void AddServiceLevelIndicatorInstrumentation_returns_same_builder_for_default_meter()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();

        var actual = builder.AddServiceLevelIndicatorInstrumentation();

        actual.Should().BeSameAs(builder);
        ServiceLevelIndicator.DefaultMeterName.Should().Be("Trellis.SLI");
    }

    [Fact]
    public void AddServiceLevelIndicatorInstrumentation_returns_same_builder_for_meter_name()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();

        var actual = builder.AddServiceLevelIndicatorInstrumentation("CustomSliMeter");

        actual.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddServiceLevelIndicatorInstrumentation_returns_same_builder_for_meter_instance()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();
        using var meter = new Meter("CustomSliMeter");

        var actual = builder.AddServiceLevelIndicatorInstrumentation(meter);

        actual.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddServiceLevelIndicatorInstrumentation_throws_for_blank_meter_name()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();

        Action act = () => builder.AddServiceLevelIndicatorInstrumentation(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddServiceLevelIndicatorInstrumentation_throws_for_null_meter()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();

        Action act = () => builder.AddServiceLevelIndicatorInstrumentation((Meter)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
