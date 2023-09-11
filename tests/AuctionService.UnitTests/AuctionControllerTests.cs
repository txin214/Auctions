using AuctionService.Controllers;
using AuctionService.Dtos;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AutoFixture;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionControllerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepo;
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly Fixture _fixture;
    private readonly AuctionsController _controller;
    private readonly IMapper _mapper;
    public AuctionControllerTests()
    {
        _fixture = new Fixture();
        _auctionRepo = new Mock<IAuctionRepository>();
        _publishEndpoint = new Mock<IPublishEndpoint>();
        var mockMapper = new MapperConfiguration(mc =>
        {
            mc.AddMaps(typeof(MappingProfiles).Assembly);
        }).CreateMapper().ConfigurationProvider;

        _mapper = new Mapper(mockMapper);
        _controller = new AuctionsController(_auctionRepo.Object, _mapper, _publishEndpoint.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext{User = Helpers.GetClaimsPrincipal()}
            }
        };
    }


    //Testing get all auctions func
    [Fact]
    public async Task GetAuctions_WithNoParams_Return10Auctions()
    {
        var auctions = _fixture.CreateMany<AuctionDto>(10).ToList();
        _auctionRepo.Setup(repo => repo.GetAuctionsAsync(null)).ReturnsAsync(auctions);
        // act
        var result = await _controller.GetAllAuctions(null);
        //assert
        Assert.Equal(10, result.Value.Count);
        Assert.IsType<ActionResult<List<AuctionDto>>>(result);
    }

    //Testing auction by Id with valid Guid
    [Fact]
    public async Task GetAuctionById_WithGuid_ReturnAuction()
    {
        var auction = _fixture.Create<AuctionDto>();
        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(auction);
        // act
        var result = await _controller.GetAuctionById(auction.Id);
        //assert
        Assert.Equal(auction.Make, result.Value.Make);
        Assert.IsType<ActionResult<AuctionDto>>(result);
    }

    //Testing auction by Id with invalid Guid
    [Fact]
    public async Task GetAuctionById_WithInvalidGuid_ReturnAuction()
    {

        _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(value: null);
        // act
        var result = await _controller.GetAuctionById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    //Testing auction by Id with invalid Guid
    [Fact]
    public async Task GetAuctionById_WithValidCreatedAucDto_ReturnsCreatedAtAction()
    {
        var auction = _fixture.Create<CreateAuctionDto>();
        _auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
        _auctionRepo.Setup(repo => repo.SaveChangeAsync()).ReturnsAsync(true);
        // act
        var result = await _controller.CreateAuction(auction);
        var createdResult = result.Result as CreatedAtActionResult;
        //assert
        Assert.NotNull(createdResult);
        Assert.Equal("GetAuctionById", createdResult.ActionName);
        Assert.IsType<AuctionDto>(createdResult.Value);
    }
}