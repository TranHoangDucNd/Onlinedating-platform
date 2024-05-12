﻿using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Globalization;
using WebDating.Data.Migrations;
using WebDating.DTOs;
using WebDating.DTOs.Post;
using WebDating.Entities;
using WebDating.Entities.PostEntities;
using WebDating.Entities.UserEntities;
using WebDating.Interfaces;
using WebDating.SignalR;

namespace WebDating.Services
{
    public class PostService : IPostService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _uow;
        private readonly UserManager<AppUser> _userManager;
        private readonly IPhotoService _photoService;
        private readonly IHubContext<CommentSignalR> _commentHubContext;

        public PostService(IMapper mapper, IUnitOfWork uow, UserManager<AppUser> userManager,
            IPhotoService photoService, IHubContext<CommentSignalR> commentHubContext)
        {
            _mapper = mapper;
            _uow = uow;
            _userManager = userManager;
            _photoService = photoService;
            _commentHubContext = commentHubContext;
        }
        public async Task<ResultDto<PostResponseDto>> Create(CreatePostDto requestDto, string username)
        {
            try
            {
                var user = await _uow.UserRepository.GetUserByUsernameAsync(username);
                Post post = new Post
                {
                    Content = requestDto.Content,
                    UserId = user.Id,
                };

                await _uow.PostRepository.Insert(post);
                await _uow.Complete();

                if (requestDto.Image != null && requestDto.Image.Count > 0)
                {
                    var images = await _photoService.AddRangerPhotoAsync(requestDto.Image);
                    foreach (var image in images)
                    {
                        var img = new ImagePost(post.Id, image.SecureUrl.AbsoluteUri, image.PublicId);
                        await _uow.PostRepository.InsertImagePost(img);
                    }

                }
                await _uow.Complete();

                var result = _mapper.Map<PostResponseDto>(post);
                return new SuccessResult<PostResponseDto>(result);
            }
            catch (Exception ex)
            {
                return new ErrorResult<PostResponseDto>(ex.Message);
            }
        }



        public async Task<ResultDto<string>> Delete(int id)
        {
            var post = await _uow.PostRepository.GetById(id);
            _uow.PostRepository.Delete(post);
            await _uow.Complete();
            return new SuccessResult<string>();
        }


        public async Task<ResultDto<PostResponseDto>> Detail(int id)
        {
            var post = await _uow.PostRepository.GetById(id);
            var result = _mapper.Map<PostResponseDto>(post);
            return new SuccessResult<PostResponseDto>(result);
        }

        public async Task<ResultDto<List<PostResponseDto>>> GetAll()
        {
            var result = await _uow.PostRepository.GetAll();
            return new SuccessResult<List<PostResponseDto>>(_mapper.Map<List<PostResponseDto>>(result));
        }


        public async Task<ResultDto<List<PostResponseDto>>> GetMyPost(string name)
        {
            var username = await _userManager.FindByNameAsync(name);
            var myPosts = await _uow.PostRepository.GetMyPost(username.Id);
            var result = _mapper.Map<List<PostResponseDto>>(myPosts);
            return new SuccessResult<List<PostResponseDto>>(result);
        }

        public async Task<ResultDto<UserShortDto>> GetUserShort(string name)
        {
            var username = await _uow.UserRepository.GetUserByUsernameAsync(name);
            return new SuccessResult<UserShortDto>(new UserShortDto()
            {
                Id = username.Id,
                FullName = username.UserName,
                KnownAs = username.KnownAs,
                Image = username.Photos.Select(x => x.Url).FirstOrDefault()
            });
        }
        
        public async Task<ResultDto<List<UserShortDto>>> GetAllUserInfo()
        {
            var users = await _uow.UserRepository.GetAllUserWithPhotosAsync();
            var listUserShort = users.Select(user => new UserShortDto()
            {
                Id = user.Id, 
                FullName = user.UserName,
                KnownAs = user.KnownAs ?? string.Empty,
                Image = user.Photos.Select(x => x.Url).FirstOrDefault() ?? string.Empty
            }).ToList();
            return new SuccessResult<List<UserShortDto>>(listUserShort);
        }

        public async Task<ResultDto<PostResponseDto>> Update(CreatePostDto requestDto, string username)
        {
            try
            {
                Post post = await _uow.PostRepository.GetById(requestDto.Id);

                post.Content = requestDto.Content;
                _uow.PostRepository.Update(post);

                if (requestDto.Image != null && requestDto.Image.Count > 0)
                {
                    await _photoService.DeleteRangerPhotoAsync(post.Images); //delete tren cloud
                    _uow.PostRepository.DeleteImages(post.Images); // delete tren db

                    var images = await _photoService.AddRangerPhotoAsync(requestDto.Image);
                    foreach (var image in images)
                    {
                        var img = new ImagePost(post.Id, image.SecureUrl.AbsoluteUri, image.PublicId);
                        await _uow.PostRepository.InsertImagePost(img);
                    }
                }
                await _uow.Complete();

                var result = _mapper.Map<PostResponseDto>(post);
                return new SuccessResult<PostResponseDto>(result);

            }
            catch (Exception ex)
            {
                return new ErrorResult<PostResponseDto>(ex.Message);
            }
        }
        //public async Task<ResultDto<List<CommentPostDto>>> CreateComment(CommentPostDto dto)
        //{
        //    var post = await _uow.PostRepository.GetById(dto.PostId);
        //    if (post is null)
        //    {
        //        return new
        //            ErrorResult<List<CommentPostDto>>("Không tìm thấy bài đọc bạn bình luận, nó có thể đã bị xóa");
        //    }


        //    //var postComment = new PostComment()
        //    //{
        //    //    PostId = post.Id,
        //    //    UserId = comment.UserId,
        //    //    Content = comment.Content
        //    //};
        //    //postComment.UpdatedAt = DateTime.UtcNow;

        //    //await _uow.PostRepository.InsertComment(postComment);
        //    //await _uow.Complete();

        //    //var comments = await GetComment(post);
        //    //await _commentHubContext.Clients.All.SendAsync("ReceiveComment", comments);


        //    #region New
        //    Comment newComment = new Comment
        //    {
        //        UserId = dto.UserId,
        //        PostId = post.Id,
        //        Content = dto.Content,

        //    };
        //    if (dto.ParentCommentId != 0)
        //    {
        //        Comment parentComment = _uow.CommentRepository.GetById(dto.ParentCommentId);
        //        if (parentComment != null)
        //        {
        //            newComment.ParentId = dto.ParentCommentId;
        //            newComment.Level = parentComment.Level + 1;
        //            if (newComment.Level > 3)
        //            {
        //                newComment.Level = 3;
        //                newComment.ParentId = parentComment.ParentId;
        //            }
        //        }
        //    }
        //    _uow.CommentRepository.Insert(newComment);
        //    await _uow.Complete();
        //    #endregion
        //    return new ResultDto<List<CommentPostDto>>();
        //}






        public async Task<Post> GetById(int postId)
        => await _uow.PostRepository.GetById(postId);

        //public async Task<ResultDto<List<CommentPostDto>>> UpdateComment(CommentPostDto comment)
        //{
        //    var postComment = await _uow.PostRepository.GetCommentById(comment.Id);
        //    var post = await _uow.PostRepository.GetById(comment.PostId);
        //    if (postComment == null)
        //    {
        //        return new ErrorResult<List<CommentPostDto>>("Không tìm thấy bài đọc bạn bình luận");
        //    }
        //    postComment.Content = comment.Content;
        //    postComment.UpdatedAt = DateTime.UtcNow;

        //    _uow.PostRepository.UpdateComment(postComment);
        //    await _uow.Complete();

        //    var comments = await GetComments(comment.PostId);
        //    await _commentHubContext.Clients.All.SendAsync("ReceiveComment", comments);

        //    return comments;
        //}
        //public async Task<ResultDto<List<CommentPostDto>>> DeleteComment(int id)
        //{
        //    var comment = await _uow.PostRepository.GetCommentById(id);
        //    var post = await _uow.PostRepository.GetById(comment.PostId);

        //    if (comment == null)
        //    {
        //        return new ErrorResult<List<CommentPostDto>>("Không tìm thấy bình luận");
        //    }

        //    _uow.PostRepository.DeleteComment(comment);
        //    await _uow.Complete();

        //    var comments = await GetComments(comment.PostId);
        //    await _commentHubContext.Clients.All.SendAsync("ReceiveComment", comments);
        //    return comments;
        //}

        public async Task<(int Likes, int Comments)> GetLikesAndCommentsCount(int postId)
        {
            var postLikes = await _uow.PostRepository.GetCountPostLikesByPostId(postId);
            var postComments = await _uow.PostRepository.GetCountPostCommentByPostId(postId);

            var likesCount = postLikes;
            var commentsCount = postComments;

            return (likesCount, commentsCount);
        }


        public async Task<ResultDto<List<PostResponseDto>>> AddOrUnLikePost(PostFpkDto postFpk)
        {
            var checkLike = await _uow.PostRepository.GetLikeByMultiId(postFpk.UserId, postFpk.PostId);
            if (checkLike is null)
            {
                PostLike postLike = new()
                {
                    UserId = postFpk.UserId,
                    PostId = postFpk.PostId,
                };
                await _uow.PostRepository.InsertPostLike(postLike);
            }
            else
            {
                _uow.PostRepository.DeletePostLike(checkLike);
            }
            await _uow.Complete();

            return await GetAll();
        }

        public async Task<bool> Report(PostReportDto postReport)
        {
            var check = await _uow.PostRepository.GetReport(postReport.UserId, postReport.PostId);
            if (check is not null)
            {
                check.Report = postReport.Report;
                check.Description = postReport.Description;

                _uow.PostRepository.UpdatePostReport(check);
                await _uow.Complete();
            }
            else
            {
                var report = new PostReportDetail()
                {
                    Checked = false,
                    Description = postReport.Description ?? "",
                    PostId = postReport.PostId,
                    UserId = postReport.UserId,
                    Report = postReport.Report,
                    ReportDate = DateTime.UtcNow
                };

                await _uow.PostRepository.InsertPostReport(report);
                await _uow.Complete();
            }

            return true;
        }

        public async Task<ResultDto<List<PostReportDto>>> GetReport()
        {
            var result = await _uow.PostRepository.GetAllReport();
            return new SuccessResult<List<PostReportDto>>(_mapper.Map<List<PostReportDto>>(result));

        }

        #region Comment
        public ResultDto<List<CommentDto>> GetCommentOfPost(int postId)
        {
            var comments = _uow.CommentRepository.GetByPostId(postId);
            return new SuccessResult<List<CommentDto>>(_mapper.Map<List<CommentDto>>(comments));
        }

        public async Task<ResultDto<string>> DeleteComment(int id)
        {
            _uow.CommentRepository.Delete(id);
            bool success = await _uow.Complete();
            return success ? new SuccessResult<string>("Thành công") : new ErrorResult<string>("Lỗi khi xóa");
        }

        public async Task<ResultDto<string>> UpdateComment(CommentPostDto dto)
        {
            var post = await _uow.PostRepository.GetById(dto.PostId);
            if (post is null)
            {
                return new ErrorResult<string>("Không tìm thấy bài đọc bạn bình luận");
            }
            var comment = _uow.CommentRepository.GetById((int)dto.Id);
            comment.Content = dto.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            _uow.CommentRepository.Update(comment);
            bool success = await _uow.Complete();

            var comments = await GetComments(comment.PostId);
            await _commentHubContext.Clients.All.SendAsync("ReceiveComment", comments);

            return success ? new SuccessResult<string>("Thành công") : new ErrorResult<string>("Lỗi khi cập nhật");
        }

        public async Task<ResultDto<string>> CreateComment(CommentPostDto dto)
        {
            var post = await _uow.PostRepository.GetById(dto.PostId);
            if (post is null)
            {
                return new ErrorResult<string>("Không tìm thấy bài đọc bạn bình luận, nó có thể đã bị xóa");
            }


            #region New
            Comment newComment = new Comment
            {
                UserId = dto.UserId,
                PostId = post.Id,
                Content = dto.Content,

            };
            if (dto.ParentCommentId != 0)
            {
                Comment parentComment = _uow.CommentRepository.GetById(dto.ParentCommentId);
                if (parentComment != null)
                {
                    newComment.ParentId = dto.ParentCommentId;
                    newComment.Level = parentComment.Level + 1;
                    if (newComment.Level > 3)
                    {
                        newComment.Level = 3;
                        newComment.ParentId = parentComment.ParentId;
                    }
                }
            }
            _uow.CommentRepository.Insert(newComment);
            bool success = await _uow.Complete();
            return success ? new SuccessResult<string>("Thành công") : new ErrorResult<string>("Lỗi khi comment");
            #endregion
        }

        public async Task<ResultDto<List<CommentVM>>> GetComments(int postId)
        {
            var comments = await _uow.CommentRepository.GetByPostId(postId);
            List<CommentVM> models = createCommentVM(postId, 0, comments, 1);

            return new SuccessResult<List<CommentVM>>(models);
        }

        List<CommentVM> createCommentVM(int postId, int parentCommentId, List<Comment> comments, int level)
        {
            List<CommentVM> items = new List<CommentVM>();
            if (level > 3)
                return items;
            IEnumerable<Comment> commentByLevel = comments.Where(it => it.ParentId == parentCommentId && it.Level == level);
            foreach (Comment cmt in commentByLevel)
            {
                CommentVM item = new CommentVM()
                {
                    Id = cmt.Id,
                    Content = cmt.Content,
                    PostId = postId,
                    UserId = cmt.UserId,
                    ParentCommentId = cmt.ParentId,
                    CreateAt = cmt.CreatedAt.ToString(CultureInfo.InvariantCulture),
                };
                item.Stats = cmt.ReactionLogs.GroupBy(it => it.ReactionType)
                    .ToDictionary(it => it.Key, it => it.Count());
                item.Descendants = createCommentVM(postId, cmt.Id, comments, level + 1);
                items.Add(item);
            }
            return items;
        }


        private List<CommentVM> createCommentVM(int postId, int parentCommentId, List<Comment> descendants, IEnumerable<ReactionLog> reactions)
        {
            List<CommentVM> items = new List<CommentVM>();
            IEnumerable<Comment> childs = descendants.Where(it => it.ParentId == parentCommentId);
            foreach (Comment child in childs)
            {
                CommentVM item = new CommentVM()
                {
                    Id = child.Id,
                    Content = child.Content,
                    PostId = postId,
                    UserId = child.UserId,
                    ParentCommentId = child.ParentId,
                    CreateAt = child.CreatedAt.ToString(CultureInfo.InvariantCulture),
                };
                item.Stats = reactions
                    .Where(it => it.CommentId == child.Id)
                    .ToDictionary(it => it.ReactionType, it => 1);
                List<Comment> replyComments = descendants
                    .Where(it => it.ParentId == child.Id)
                    .ToList();
                while (replyComments.Count > 0)
                {
                    item.Descendants = createCommentVM(postId, child.Id, replyComments, reactions);
                }
                items.Add(item);
            }
            return items;
        }
        #endregion


        #region Thả react
        public async Task<ResultDto<string>> ReactComment(ReactionRequest request)
        {
            ReactionLog react = _uow.ReactionLogRepository.GetReactUserByComment(request.UserId, request.TargetId);
            if (react is null)
            {
                react = new ReactionLog
                {
                    UserId = request.UserId,
                    CommentId = request.TargetId,
                    ReactionType = request.ReactionType,
                    Target = ReactTarget.Comment,
                };
                _uow.ReactionLogRepository.Insert(react);
            }
            else
            {
                if (react.ReactionType == request.ReactionType)
                    _uow.ReactionLogRepository.Remove(react);
                else
                    react.ReactionType = request.ReactionType;
            }

            bool success = await _uow.Complete();
            return success ? new SuccessResult<string>("Thành công") : new ErrorResult<string>("Lỗi tương tác cảm xúc bình luận");
        }
        public async Task<ResultDto<string>> ReactPost(ReactionRequest request)
        {
            ReactionLog react = _uow.ReactionLogRepository.GetReactUserByPost(request.UserId, request.TargetId);
            if (react is null)
            {
                react = new ReactionLog
                {
                    UserId = request.UserId,
                    PostId = request.TargetId,
                    ReactionType = request.ReactionType,
                    Target = ReactTarget.Post,
                };
                _uow.ReactionLogRepository.Insert(react);
            }
            else
            {
                if (react.ReactionType == request.ReactionType)
                    _uow.ReactionLogRepository.Remove(react);
                else
                    react.ReactionType = request.ReactionType;
            }
            bool success = await _uow.Complete();
            return success ? new SuccessResult<string>("Thành công") : new ErrorResult<string>("Lỗi tương tác cảm xúc bài viết");
        }

        public async Task<ResultDto<List<ReactionLogVM>>> GetDetailReaction(int targetId)
        {
            List<ReactionLogVM> vms = new List<ReactionLogVM>();
            List<ReactionLog> reactions = await _uow.ReactionLogRepository.GetDetailReaction(targetId);

            List<AppUser> userCommented = await _uow.UserRepository.GetMany(reactions.Select(it => it.UserId));
            foreach (var react in reactions)
            {
                var user = userCommented.Find(it => it.Id == react.UserId);
                if (user != null)
                {
                    var vm = new ReactionLogVM()
                    {
                        Type = react.ReactionType,
                        DisplayName = Convert.ToString(react.ReactionType),
                        UserFullName = user.KnownAs,
                        UserId = user.Id,
                    };
                    vms.Add(vm);
                }

            }
            return new SuccessResult<List<ReactionLogVM>>() { ResultObj = vms };
        }
        #endregion
    }
}
