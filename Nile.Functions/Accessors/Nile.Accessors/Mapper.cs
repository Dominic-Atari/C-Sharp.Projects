//global using System;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Nile.Common.InternalDTOs;
using Nile.Utilities;
using Nile.Utilities.AzureSdk;
using Nile.Accessors.DataContracts;
using EF = Nile.Database.Entities;


namespace Nile.Accessors
{
    /// <summary>
    /// Custom mapper wrapper around AutoMapper with project-specific configuration
    /// </summary>
    internal class Mapper : IMapper
    {
        private readonly ILogger<Mapper> _logger;
        private readonly IDateUtility _dateUtility;
        public IConfigurationProvider Configuration { get; }
        
        private AutoMapper.IMapper AutoMapper { get; set; }

        public Mapper(ILogger<Mapper> logger, 
            IDateUtility dateUtility, 
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _dateUtility = dateUtility;

            // Pass ILoggerFactory as second parameter.
            var config = new MapperConfiguration(cfg =>
            {
                AddUserMappings(cfg);
                AddPostMappings(cfg);
            }, loggerFactory);

            Configuration = config;
            AutoMapper = config.CreateMapper();
        }

        // Mapping configuration methods
        private void AddUserMappings(IMapperConfigurationExpression cfg)
        {
            // Entity to Response DTO
            cfg.CreateMap<EF.User, UserResponseBase>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => new[] { src.Id }))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.UtcDateTime));

            cfg.CreateMap<EF.User, StoreUserResponseBase>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => new[] { src.Id }))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.UtcDateTime));

            // Note: Creation/update mappings to EF.User are handled manually in the accessor now
        }

        private void AddPostMappings(IMapperConfigurationExpression cfg)
        {
            // EF.Post -> Accessor DataContract used by PostAccessor
            cfg.CreateMap<EF.Post, PostData>()
                .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => src.PostId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId));
        }
        
        // IMapper implementation
        public void Map(object source, object destination)
        {
            if (source == null || destination == null)
            {
                _logger.LogWarning("Attempted to map null objects");
                return;
            }

            try
            {
                AutoMapper.Map(source, destination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error mapping {source.GetType().Name} to {destination.GetType().Name}");
                throw;
            }
        }

        // Generic mapping
        public T Map<T>(object source)
        {
            if (source == null)
            {
                _logger.LogWarning("Attempted to map null source");
                return default!;
            }

            try
            {
                return AutoMapper.Map<T>(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error mapping {source.GetType().Name} to {typeof(T).Name}");
                throw;
            }
        }
    }
}
