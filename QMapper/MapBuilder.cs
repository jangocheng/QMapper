﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QMapper
{
    /// <summary>
    /// 映射创建者
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    class MapBuilder<TSource> : ICompileMapBuilder<TSource>, IDynamicMapBuilder<TSource> where TSource : class
    {
        /// <summary>
        /// 数据源
        /// </summary>
        private readonly TSource source;

        /// <summary>
        /// 包含的属性名称
        /// </summary>
        private HashSet<string> includeMembers;

        /// <summary>
        /// 源类型的所有属性
        /// </summary>
        private static readonly PropertyInfo[] sourceProperies = typeof(TSource).GetProperties();

        /// <summary>
        /// 源类型的所有属性名称
        /// </summary>
        private static readonly string[] sourceMembers = sourceProperies.Select(item => item.Name).ToArray();


        /// <summary>
        /// 构建编译形映射创建者
        /// </summary>
        public MapBuilder()
        {
        }

        /// <summary>
        /// 构建编译形映射创建者
        /// </summary>
        /// <param name="includeMembers">映射的的属性名称</param>
        public MapBuilder(IEnumerable<string> includeMembers)
            : this(null, includeMembers)
        {
        }

        /// <summary>
        /// 构建动态映射创建者
        /// </summary>
        /// <param name="source">数据源</param>
        public MapBuilder(TSource source)
            : this(source, null)
        {
        }

        /// <summary>
        /// 构建动态映射创建者
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="includeMembers">映射的的属性名称</param>
        public MapBuilder(TSource source, IEnumerable<string> includeMembers)
        {
            this.source = source;
            if (includeMembers != null)
            {
                this.includeMembers = new HashSet<string>(includeMembers, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 忽略映射的字段
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="ignoreKey">忽略的字段</param>
        /// <returns></returns>
        ICompileMapBuilder<TSource> ICompileMapBuilder<TSource>.Ignore<TKey>(Expression<Func<TSource, TKey>> ignoreKey)
        {
            this.Ignore<TKey>(ignoreKey);
            return this;
        }

        /// <summary>
        /// 忽略映射的字段
        /// </summary>
        /// <param name="memberName">忽略的字段</param>
        /// <returns></returns>
        ICompileMapBuilder<TSource> ICompileMapBuilder<TSource>.Ignore(params string[] memberName)
        {
            this.Ignore(memberName);
            return this;
        }

        /// <summary>
        /// 忽略映射的字段
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="ignoreKey">忽略的字段</param>
        /// <returns></returns>
        IDynamicMapBuilder<TSource> IDynamicMapBuilder<TSource>.Ignore<TKey>(Expression<Func<TSource, TKey>> ignoreKey)
        {
            this.Ignore<TKey>(ignoreKey);
            return this;
        }

        /// <summary>
        /// 忽略映射的字段
        /// </summary>
        /// <param name="memberName">忽略的字段</param>
        /// <returns></returns>
        IDynamicMapBuilder<TSource> IDynamicMapBuilder<TSource>.Ignore(params string[] memberName)
        {
            this.Ignore(memberName);
            return this;
        }

        /// <summary>
        /// 忽略映射的字段
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="ignoreKey">忽略的字段</param>
        /// <exception cref="ArgumentNullException"></exception>        
        private void Ignore<TKey>(Expression<Func<TSource, TKey>> ignoreKey)
        {
            if (ignoreKey == null)
            {
                throw new ArgumentNullException(nameof(ignoreKey));
            }

            if (ignoreKey.Body is MemberExpression body)
            {
                this.Ignore(body.Member.Name);
            }
        }

        /// <summary>
        /// 忽略映射的字段
        /// </summary>
        /// <param name="memberName">忽略的字段</param>      
        private void Ignore(params string[] memberName)
        {
            if (this.includeMembers == null)
            {
                this.includeMembers = new HashSet<string>(sourceMembers, StringComparer.OrdinalIgnoreCase);
            }

            foreach (var item in memberName)
            {
                this.includeMembers.Remove(item);
            }
        }

        /// <summary>
        /// 为指定目标类型编译映射
        /// 返回编译后的映射
        /// </summary>
        /// <typeparam name="TTarget">目标类型</typeparam>
        /// <returns></returns>
        public IMapper<TSource, TTarget> Compile<TTarget>() where TTarget : class
        {
            try
            {
                return new Mapper<TTarget>(this.includeMembers);
            }
            catch (MapException)
            {
                throw;
            }
            catch (TypeInitializationException ex)
            {
                throw new MapException(typeof(TSource), typeof(TTarget), ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new MapException(typeof(TSource), typeof(TTarget), ex);
            }
        }

        /// <summary>
        /// 映射到目标对象      
        /// </summary>
        /// <typeparam name="TTarget">TTarget</typeparam>     
        /// <returns></returns>
        public TTarget To<TTarget>() where TTarget : class, new()
        {
            return this.To(new TTarget());
        }

        /// <summary>
        /// 映射到目标对象       
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        public TTarget To<TTarget>(TTarget target) where TTarget : class
        {
            try
            {
                return Mapper<TTarget>.DynamicMap(this.source, target, this.includeMembers);
            }
            catch (MapException)
            {
                throw;
            }
            catch (TypeInitializationException ex)
            {
                throw new MapException(typeof(TSource), typeof(TTarget), ex.InnerException);
            }
            catch (Exception ex)
            {
                throw new MapException(typeof(TSource), typeof(TTarget), ex);
            }
        }

        /// <summary>
        /// 表示对象映射
        /// </summary>
        /// <typeparam name="TTarget">目标类型</typeparam>
        private class Mapper<TTarget> : IMapper<TSource, TTarget> where TTarget : class
        {
            /// <summary>
            /// 所有映射属性
            /// </summary>
            private static readonly MapItem[] allMapItems;

            /// <summary>
            /// 静态构造器
            /// </summary>
            static Mapper()
            {
                var q = from s in sourceProperies
                        join t in typeof(TTarget).GetProperties()
                        on s.Name.ToLower() equals t.Name.ToLower()
                        let map = new MapItem(s, t)
                        where map.IsEnable
                        select map;

                allMapItems = q.ToArray();
            }

            /// <summary>
            /// 要映射的属性
            /// </summary>
            private readonly MapItem[] mapItems;

            /// <summary>
            /// 对象映射
            /// </summary>
            /// <param name="members">映射的字段</param>
            public Mapper(ICollection<string> members)
            {
                if (members == null)
                {
                    this.mapItems = allMapItems;
                }
                else
                {
                    this.mapItems = allMapItems.Where(item => members.Contains(item.Name)).ToArray();
                }
            }

            /// <summary>
            /// 将source映射到新的Ttarget类型
            /// </summary>
            /// <param name="source">源对象</param>   
            /// <returns></returns>
            public TTarget Map(TSource source)
            {
                var target = Activator.CreateInstance<TTarget>();
                return this.Map(source, target);
            }

            /// <summary>
            /// 将source映射到新的Ttarget类型
            /// </summary>
            /// <param name="source">源对象</param>   
            /// <returns></returns>
            public TTarget[] Map(IEnumerable<TSource> source)
            {
                return source.Select(item => this.Map(item)).ToArray();
            }

            /// <summary>
            /// 映射到目标对象
            /// </summary>
            /// <param name="source">源对象</param>
            /// <param name="target">目标对象</param>
            /// <returns></returns>
            public TTarget Map(TSource source, TTarget target)
            {
                if (source == null || target == null)
                {
                    return target;
                }

                foreach (var item in this.mapItems)
                {
                    item.Invoke(source, target);
                }

                return target;
            }

            /// <summary>
            /// 动态映射
            /// </summary>
            /// <param name="source">源</param>
            /// <param name="target">目标</param>
            /// <param name="members">映射的属性</param>
            /// <returns></returns>
            public static TTarget DynamicMap(TSource source, TTarget target, HashSet<string> members)
            {
                if (target == null)
                {
                    return null;
                }

                if (members == null)
                {
                    foreach (var item in allMapItems)
                    {
                        item.Invoke(source, target);
                    }
                }
                else
                {
                    foreach (var item in allMapItems)
                    {
                        if (members.Contains(item.Name) == true)
                        {
                            item.Invoke(source, target);
                        }
                    }
                }

                return target;
            }


            /// <summary>
            /// 表示映射属性项
            /// </summary>
            private class MapItem
            {
                /// <summary>
                /// 映射委托
                /// </summary>
                private readonly Action<TSource, TTarget> action;

                /// <summary>
                /// 获取属性名称
                /// </summary>
                public string Name { get; }

                /// <summary>
                /// 获取是否可用
                /// </summary>
                public bool IsEnable { get; }

                /// <summary>
                /// 获取源属性
                /// </summary>
                public PropertyInfo SourceProperty { get; }

                /// <summary>
                /// 获取目标属性
                /// </summary>
                public PropertyInfo TargetProperty { get; }

                /// <summary>
                /// 映射属性
                /// </summary>
                /// <param name="sourceProperty">源属性</param>
                /// <param name="targetProperty">目标属性</param>
                public MapItem(PropertyInfo sourceProperty, PropertyInfo targetProperty)
                {
                    this.SourceProperty = sourceProperty;
                    this.TargetProperty = targetProperty;
                    this.Name = sourceProperty.Name;

                    this.action = this.CreateAction();
                    this.IsEnable = this.action != null;
                }

                /// <summary>
                /// 创建映射委托
                /// (source,target) => target.Name = source.Name;
                /// </summary>                  
                /// <returns></returns>
                private Action<TSource, TTarget> CreateAction()
                {
                    if (this.SourceProperty.GetGetMethod() == null || this.TargetProperty.GetSetMethod() == null)
                    {
                        return null;
                    }

                    var source = Expression.Parameter(typeof(TSource), "source");
                    var target = Expression.Parameter(typeof(TTarget), "target");

                    var value = Expression.Property(source, this.SourceProperty);
                    var context = new Context(value, this.SourceProperty, this.TargetProperty);
                    var valueCasted = Converter.Convert(context);

                    var body = Expression.Assign(Expression.Property(target, this.TargetProperty), valueCasted);
                    return Expression.Lambda<Action<TSource, TTarget>>(body, source, target).Compile();
                }

                /// <summary>
                /// 执行映射
                /// </summary>
                /// <param name="source">源</param>
                /// <param name="target">目标</param>
                public void Invoke(TSource source, TTarget target)
                {
                    this.action.Invoke(source, target);
                }

                /// <summary>
                /// 转换为字符串
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return this.Name;
                }
            }
        }
    }
}
