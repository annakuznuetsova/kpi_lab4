using Moq;
using Subscription_Service.Models;
using Subscription_Service.Services;
using Subscription_Service.Services.Interfaces;
using Xunit;

namespace SubscriptionServiceTests
{
    public class SubscriptionServiceTests
    {
        private readonly Mock<IMemberRepository> _repo;
        private readonly Mock<IPaymentService> _payment;
        private readonly Mock<INotificationService> _notify;
        private readonly SubscriptionService _service;

        public SubscriptionServiceTests()
        {
            _repo = new Mock<IMemberRepository>();
            _payment = new Mock<IPaymentService>();
            _notify = new Mock<INotificationService>();
            _service = new SubscriptionService(_repo.Object, _payment.Object, _notify.Object);
        }

        /// <summary>
        /// Якщо користувача не знайдено, RenewSubscription має кинути ArgumentException.
        /// </summary>
        [Fact]
        public void RenewSubscription_ShouldThrowArgumentException_WhenMemberNotFound()
        {
            _repo.Setup(r => r.GetById(1)).Returns((Member?)null);

            Assert.Throws<ArgumentException>(() =>
                _service.RenewSubscription(1, 100m, 30));
        }

        /// <summary>
        /// Якщо оплата не проходить, RenewSubscription повертає False
        /// і не викликає Update та SendNotification.
        /// </summary>
        [Fact]
        public void RenewSubscription_ShouldReturnFalse_WhenPaymentFails()
        {
            var member = new Member { Id = 2, IsActive = false };
            _repo.Setup(r => r.GetById(2)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(2, 100m)).Returns(false);

            var result = _service.RenewSubscription(2, 100m, 30);

            Assert.False(result);
            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Never);
            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Якщо оплата успішна, RenewSubscription має
        /// оновити SubscriptionEnd, встановити IsActive = true,
        /// викликати Update та SendNotification.
        /// </summary>
        [Fact]
        public void RenewSubscription_ShouldSucceed_WhenPaymentVerified()
        {
            var member = new Member { Id = 3, IsActive = false, SubscriptionEnd = null };
            _repo.Setup(r => r.GetById(3)).Returns(member);
            _payment.Setup(p => p.VerifyPayment(3, 200m)).Returns(true);

            var result = _service.RenewSubscription(3, 200m, 30);

            Assert.True(result);

            Assert.True(member.IsActive);
            Assert.NotNull(member.SubscriptionEnd);
            Assert.True(member.SubscriptionEnd > DateTime.Now); 

            _repo.Verify(r => r.Update(member), Times.Once);

            _notify.Verify(n =>
                n.SendNotification("Subscription renewed!", 3),
                Times.Once);
        }

        /// <summary>
        /// Деактивація користувачів з простроченою підпискою:
        /// IsActive має бути false, Update викликається, Notification відправлено.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_ShouldDeactivateExpiredMember()
        {
            var expiredMember = new Member
            {
                Id = 5,
                IsActive = true,
                SubscriptionEnd = DateTime.Now.AddDays(-1) // вчора → прострочено
            };

            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { expiredMember });

            _service.DeactivateExpiredMembers();

            Assert.False(expiredMember.IsActive);

            _repo.Verify(r => r.Update(expiredMember), Times.Once);

            _notify.Verify(n =>
                n.SendNotification("Membership expired", 5),
                Times.Once);
        }

        /// <summary>
        /// Якщо підписка ще активна, користувач не повинен деактивуватися.
        /// Update та Notification не викликаються.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_ShouldNotDeactivateActiveMember()
        {
            var activeMember = new Member
            {
                Id = 6,
                IsActive = true,
                SubscriptionEnd = DateTime.Now.AddDays(5) // ще активна
            };

            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { activeMember });

            _service.DeactivateExpiredMembers();

            Assert.True(activeMember.IsActive);

            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Never);

            _notify.Verify(n =>
                n.SendNotification(It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);
        }

        /// <summary>
        /// Якщо SubscriptionEnd є null, користувач не деактивується.
        /// </summary>
        [Fact]
        public void DeactivateExpiredMembers_ShouldIgnoreMembersWithNoSubscriptionEnd()
        {
            var member = new Member
            {
                Id = 7,
                IsActive = true,
                SubscriptionEnd = null
            };

            _repo.Setup(r => r.GetAll()).Returns(new List<Member> { member });

            _service.DeactivateExpiredMembers();

            Assert.True(member.IsActive);

            _repo.Verify(r => r.Update(It.IsAny<Member>()), Times.Never);
            _notify.Verify(n => n.SendNotification(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
    }
}
