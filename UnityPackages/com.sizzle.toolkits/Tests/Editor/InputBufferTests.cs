using NUnit.Framework;
using Sizzle.Toolkits.Input;

namespace Sizzle.Toolkits.Tests.Input
{
    [TestFixture]
    public class InputBufferTests
    {
        [Test]
        public void ManualTick_ExpiresInputAfterDurationElapsed()
        {
            var buffer = new InputBuffer(0.25f, InputBufferTimeScaleMode.ManualTick);

            buffer.Buffer("Attack");
            Assert.IsTrue(buffer.Has("Attack"));
            Assert.IsTrue(buffer.IsAnyBuffered);
            Assert.AreEqual(0.25f, buffer.GetRemaining("Attack"), 0.0001f);

            buffer.Tick(0.15f);
            Assert.IsTrue(buffer.Has("Attack"));
            Assert.AreEqual(0.10f, buffer.GetRemaining("Attack"), 0.0001f);

            // 0 이하 deltaTime은 시간을 진행시키지 않아야 함
            buffer.Tick(0f);
            buffer.Tick(-0.5f);
            Assert.AreEqual(0.10f, buffer.GetRemaining("Attack"), 0.0001f);

            buffer.Tick(0.11f);
            Assert.IsFalse(buffer.Has("Attack"));
            Assert.IsFalse(buffer.IsAnyBuffered);
            Assert.AreEqual(0f, buffer.GetRemaining("Attack"), 0.0001f);
            Assert.IsFalse(buffer.Consume("Attack"));
        }

        [Test]
        public void ManualTick_ConsumeReturnsCustomDataAndInvalidatesEntry()
        {
            var buffer = new InputBuffer(0.2f, InputBufferTimeScaleMode.ManualTick);
            buffer.Buffer("Dodge", customDuration: 0.5f, customData: 42);

            buffer.Tick(0.3f);
            Assert.IsTrue(buffer.Consume("dodge", out object data));
            Assert.AreEqual(42, data);

            Assert.IsFalse(buffer.Has("Dodge"));
            Assert.IsFalse(buffer.Consume("Dodge"));
        }

        [Test]
        public void TimeScaleMode_ChangingModeClearsExistingBuffer()
        {
            var buffer = new InputBuffer<int>(0.5f, timeScaleMode: InputBufferTimeScaleMode.ManualTick);
            buffer.Buffer(1);
            Assert.IsTrue(buffer.HasAction(1));

            // 동일 모드 재할당 시 버퍼 유지
            buffer.TimeScaleMode = InputBufferTimeScaleMode.ManualTick;
            Assert.IsTrue(buffer.HasAction(1));

            // 다른 모드로 전환 시 버퍼 초기화
            buffer.TimeScaleMode = InputBufferTimeScaleMode.Scaled;
            Assert.IsFalse(buffer.HasAction(1));
            Assert.IsFalse(buffer.IsAnyBuffered);
        }

        [Test]
        public void Constructor_RespectsInitialTimeScaleMode()
        {
            var unscaled = new InputBuffer<string>();
            Assert.AreEqual(InputBufferTimeScaleMode.Unscaled, unscaled.TimeScaleMode);

            var scaled = new InputBuffer<string>(0.2f, InputBufferTimeScaleMode.Scaled);
            Assert.AreEqual(InputBufferTimeScaleMode.Scaled, scaled.TimeScaleMode);

            var manual = new InputBuffer(0.2f, InputBufferTimeScaleMode.ManualTick);
            Assert.AreEqual(InputBufferTimeScaleMode.ManualTick, manual.TimeScaleMode);
        }
    }
}
