using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;
using Xunit;

namespace SubscriptionServiceTests
{
    public class MemberServiceTests
    {
        private readonly Mock<IMemberRepository> _repoMock;
        private readonly MemberService _service;

        public MemberServiceTests()
        {
            _repoMock = new Mock<IMemberRepository>();
            _service = new MemberService(_repoMock.Object);
        }

        /// <summary>
        /// Перевіряємо, що GetMember повертає існуючого користувача.
        /// </summary>
        [Fact]
        public void GetMember_ShouldReturnMember_WhenMemberExists()
        {
            var member = new Member { Id = 1, Name = "Test", IsActive = true };
            _repoMock.Setup(r => r.GetById(1)).Returns(member);

            var result = _service.GetMember(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        /// <summary>
        /// Якщо користувач не існує, GetMember має повернути null.
        /// </summary>
        [Fact]
        public void GetMember_ShouldReturnNull_WhenMemberDoesNotExist()
        {
            _repoMock.Setup(r => r.GetById(5)).Returns((Member?)null);

            var result = _service.GetMember(5);

            Assert.Null(result);
        }

        /// <summary>
        /// IsActive повертає True для активного користувача.
        /// </summary>
        [Fact]
        public void IsActive_ShouldReturnTrue_WhenMemberIsActive()
        {
            var member = new Member { Id = 2, Name = "Active", IsActive = true };
            _repoMock.Setup(r => r.GetById(2)).Returns(member);

            var result = _service.IsActive(2);

            Assert.True(result);
        }

        /// <summary>
        /// IsActive повертає False для користувача, який не активний.
        /// </summary>
        [Fact]
        public void IsActive_ShouldReturnFalse_WhenMemberIsNotActive()
        {
            var member = new Member { Id = 3, Name = "Inactive", IsActive = false };
            _repoMock.Setup(r => r.GetById(3)).Returns(member);

            var result = _service.IsActive(3);

            Assert.False(result);
        }

        /// <summary>
        /// IsActive повертає False, якщо користувача не знайдено.
        /// </summary>
        [Fact]
        public void IsActive_ShouldReturnFalse_WhenMemberDoesNotExist()
        {
            _repoMock.Setup(r => r.GetById(10)).Returns((Member?)null);

            var result = _service.IsActive(10);

            Assert.False(result);
        }
    }
}
