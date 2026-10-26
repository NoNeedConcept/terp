namespace Terp.Tests;

public sealed class DateTimeExtensionsTests
{
    private sealed class TestTimeProvider(DateTimeOffset utcNow, TimeZoneInfo? zone = null) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }

        public override TimeZoneInfo LocalTimeZone { get; } = zone ?? TimeZoneInfo.Utc;
    }

    private static readonly TimeZoneInfo _plus2 =
        TimeZoneInfo.CreateCustomTimeZone("plus2", TimeSpan.FromHours(2), "plus2", "plus2");

    private static readonly DateTimeOffset _now = new(2019, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsToday_CurrentLocalDay_ReturnsTrueOnlyForThatDay()
    {
        var time = new TestTimeProvider(_now);

        Assert.True(new DateTime(2019, 9, 18, 0, 0, 0).IsToday(time));
        Assert.True(new DateTime(2019, 9, 18, 23, 59, 59).IsToday(time));
        Assert.False(new DateTime(2019, 9, 17, 23, 59, 59).IsToday(time));
        Assert.False(new DateTime(2019, 9, 19).IsToday(time));
    }

    [Fact]
    public void IsToday_UtcKind_ConvertsToProviderLocalTime()
    {
        var time = new TestTimeProvider(new DateTimeOffset(2019, 9, 18, 23, 0, 0, TimeSpan.Zero), _plus2);

        // local "now" is 2019-09-19 01:00
        Assert.True(new DateTime(2019, 9, 18, 22, 30, 0, DateTimeKind.Utc).IsToday(time));
        Assert.False(new DateTime(2019, 9, 18, 21, 30, 0, DateTimeKind.Utc).IsToday(time));
        Assert.False(new DateTime(2019, 9, 18, 22, 30, 0, DateTimeKind.Unspecified).IsToday(time));
    }

    [Fact]
    public void IsToday_DefaultProvider_UsesSystemClock()
    {
        Assert.True(DateTime.Now.IsToday());
        Assert.True(DateTime.UtcNow.IsToday());
    }

    [Fact]
    public void IsBetween_DateInsideRange_ReturnsTrue()
    {
        var dateTime = new DateTime(2019, 9, 18);
        Assert.True(dateTime.IsBetween(new DateTime(2019, 1, 1), new DateTime(2020, 1, 1)));
    }

    [Fact]
    public void IsBetween_InvertedBounds_ReturnsTrue()
    {
        var dateTime = new DateTime(2019, 9, 18);
        Assert.True(dateTime.IsBetween(new DateTime(2020, 1, 1), new DateTime(2019, 1, 1)));
    }

    [Fact]
    public void IsBetween_DateOnBounds_ReturnsTrue()
    {
        var dateTime = new DateTime(2019, 9, 18);

        // upper edge
        Assert.True(dateTime.IsBetween(new DateTime(2019, 1, 1), new DateTime(2019, 9, 18)));

        // lower edge
        Assert.True(dateTime.IsBetween(new DateTime(2019, 9, 18), new DateTime(2020, 1, 1)));
    }

    [Theory]
    [InlineData(2019, 9, 16, true)]
    [InlineData(2019, 9, 17, true)]
    [InlineData(2019, 9, 18, true)]
    [InlineData(2019, 9, 19, true)]
    [InlineData(2019, 9, 20, true)]
    [InlineData(2019, 9, 21, false)]
    [InlineData(2019, 9, 22, false)]
    public void IsWorkday_WeekdayOrWeekend_ReturnsTrueOnlyMondayToFriday(int year, int month, int day, bool isWorkday)
    {
        var dateTime = new DateTime(year, month, day);
        Assert.Equal(isWorkday, dateTime.IsWorkday());
    }

    [Theory]
    [InlineData(2019, 9, 16, false)]
    [InlineData(2019, 9, 17, false)]
    [InlineData(2019, 9, 18, false)]
    [InlineData(2019, 9, 19, false)]
    [InlineData(2019, 9, 20, false)]
    [InlineData(2019, 9, 21, true)]
    [InlineData(2019, 9, 22, true)]
    public void IsWeekend_WeekdayOrWeekend_ReturnsTrueOnlySaturdayAndSunday(int year, int month, int day, bool isWeekend)
    {
        var dateTime = new DateTime(year, month, day);
        Assert.Equal(isWeekend, dateTime.IsWeekend());
    }

    [Fact]
    public void IsWorkday_PublicHoliday_ReturnsTrue()
    {
        // 2019-12-25 is a Wednesday and a public holiday in most countries
        Assert.True(new DateTime(2019, 12, 25).IsWorkday());
    }

    [Fact]
    public void IsPast_ProviderTime_ComparesAgainstProviderNow()
    {
        var time = new TestTimeProvider(_now);

        Assert.True(_now.UtcDateTime.AddMinutes(-1).IsPast(time));
        Assert.False(_now.UtcDateTime.AddDays(1).IsPast(time));
        Assert.False(_now.UtcDateTime.IsPast(time));
    }

    [Fact]
    public void IsInFuture_ProviderTime_ComparesAgainstProviderNow()
    {
        var time = new TestTimeProvider(_now);

        Assert.True(_now.UtcDateTime.AddMinutes(20).IsInFuture(time));
        Assert.True(_now.UtcDateTime.AddMinutes(20).ToLocalTime().IsInFuture(time));
        Assert.False(_now.UtcDateTime.IsInFuture(time));
        Assert.False(_now.UtcDateTime.AddMinutes(-1).IsInFuture(time));
    }

    [Fact]
    public void IsPastAndIsInFuture_UnspecifiedKind_InterpretedInProviderTimeZone()
    {
        var time = new TestTimeProvider(_now, _plus2);

        // 13:59 local (+2) is 11:59 UTC, which is before 12:00 UTC
        Assert.True(new DateTime(2019, 9, 18, 13, 59, 0).IsPast(time));
        Assert.False(new DateTime(2019, 9, 18, 13, 59, 0).IsInFuture(time));
        Assert.True(new DateTime(2019, 9, 18, 14, 1, 0).IsInFuture(time));
    }

    [Fact]
    public void IsPastAndIsInFuture_UtcKind_TreatedAsUtc()
    {
        var time = new TestTimeProvider(_now, _plus2);

        Assert.True(new DateTime(2019, 9, 18, 11, 59, 0, DateTimeKind.Utc).IsPast(time));
        Assert.True(new DateTime(2019, 9, 18, 12, 1, 0, DateTimeKind.Utc).IsInFuture(time));
    }

    [Fact]
    public void IsPastAndIsInFuture_DefaultProvider_UseSystemClock()
    {
        Assert.True(DateTime.UtcNow.AddMinutes(20).IsInFuture());
        Assert.True(DateTime.Now.AddMinutes(20).IsInFuture());
        Assert.True(DateTime.Now.AddMinutes(-20).IsPast());
        Assert.False(DateTime.Now.AddDays(1).IsPast());
    }
}
