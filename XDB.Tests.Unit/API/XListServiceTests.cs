
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using XDB.API;
using XDB.BLL;
using XDB.DAL;
using XDB.DataObjects;
using XDB.Constants;
using XDB.Interfaces;

using XDB.Tests.Unit.Factories;
using XDB.Tests.Unit.Fakes;

using NSubstitute;

namespace XDB.Tests.Unit.API
{

    [TestFixture]
    public class XListServiceTests
    {

        [Test]
        public void Foo()
        {

            XList xList = new XListFactory().BuildXListWithOnlyOneValue();

            var fakeRepo = Substitute.For<IXListRepository>();
            fakeRepo.Get(new Guid("99999999-9999-9999-9999-999999999999")).Returns(new XList() { Id = new Guid("99999999-9999-9999-9999-999999999999") });

            var save = Substitute.For<IXListRepository>();
            var something = new XListDomain(save);
            something.Save(xList, XUserIds.Admin);
            save.Received().Save(xList);   

            XListDomain domain = new XListDomain(fakeRepo);
            Assert.AreEqual(new Guid("99999999-9999-9999-9999-999999999999"), domain.Get(new Guid("99999999-9999-9999-9999-999999999999")).Id);
            Assert.That(!domain.Get(new Guid("99999999-9999-9999-9999-999999999999")).IsDirty);
            Assert.That(!domain.Get(new Guid("99999999-9999-9999-9999-999999999999")).IsNew);

        }

    }

}