using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using UdemusDateus.DTOs;
using UdemusDateus.Entities;
using UdemusDateus.Extensions;
using UdemusDateus.Helpers;
using UdemusDateus.Interfaces;

namespace UdemusDateus.Controllers;

public class UsersController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPhotoService _photoService;

    public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _photoService = photoService;
    }

    /// <summary>
    /// Returns all users that conform to passed in params.
    /// </summary>
    /// <param name="userParams"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
    {
        var gender = await _unitOfWork.UserRepository.GetUserGender(User.GetUsername());

        userParams.CurrentUsername = User.GetUsername();

        if (string.IsNullOrEmpty(userParams.Gender))
        {
            userParams.Gender = gender == "male" ? "female" : "male";
        }

        var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);

        Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

        return Ok(users);
    }


    /// <summary>
    /// Gets specified user from username.
    /// </summary>
    /// <param name="userName">Username of user to return</param>
    /// <returns></returns>
    [HttpGet("{userName}", Name = "GetUser")]
    public async Task<ActionResult<MemberDto>> GetUser(string userName)
    {
        return await _unitOfWork.UserRepository.GetMemberByUserNameAsync(userName);
    }

    /// <summary>
    /// Updates current user with passed in information.
    /// </summary>
    /// <param name="memberUpdateDto"></param>
    /// <returns></returns>
    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(User.GetUsername());

        _mapper.Map(memberUpdateDto, user);
        _unitOfWork.UserRepository.Update(user);

        if (await _unitOfWork.Complete()) return NoContent();

        return BadRequest("Failed to update user!");
    }

    /// <summary>
    /// Adds photo to user.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(User.GetUsername());

        var result = await _photoService.AddPhotoAsync(file);

        if (result.Error != null) return BadRequest(result.Error.Message);

        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };

        if (user.Photos.Count == 0)
        {
            photo.IsMain = true;
        }

        user.Photos.Add(photo);

        if (await _unitOfWork.Complete())
        {
            return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDto>(photo));
        }

        return BadRequest("Problem adding photo!");
    }

    /// <summary>
    /// Sets the main photo used for current user.
    /// </summary>
    /// <param name="photoId">Id of photo to be main photo</param>
    /// <returns></returns>
    [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(User.GetUsername());

        var photo = user.Photos.FirstOrDefault(photo => photo.Id == photoId);

        if (photo is { IsMain: true }) return BadRequest("Photo is already Main photo!");

        var currentMain = user.Photos.FirstOrDefault(photo => photo.IsMain);

        if (currentMain != null) currentMain.IsMain = false;
        if (photo != null) photo.IsMain = true;

        if (await _unitOfWork.Complete()) return NoContent();

        return BadRequest("Failed to set main photo!");
    }

    /// <summary>
    /// Deletes photo.
    /// </summary>
    /// <param name="photoId">Id of photo to be deleted</param>
    /// <returns></returns>
    [HttpDelete("delete-photo/{photoId}")]
    public async Task<ActionResult> DeletePhoto(int photoId)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(User.GetUsername());

        var photo = user.Photos.FirstOrDefault(photo => photo.Id == photoId);

        if (photo == null) return NotFound();

        if (photo.IsMain) return BadRequest("You cannot delete your main photo!");

        if (photo.PublicId != null)
        {
            var res = await _photoService.DeletePhotoAsync(photo.PublicId);

            if (res.Error != null) return BadRequest(res.Error.Message);
        }

        user.Photos.Remove(photo);

        if (await _unitOfWork.Complete()) return Ok();

        return BadRequest("An error has occured trying to delete photo!");
    }
}