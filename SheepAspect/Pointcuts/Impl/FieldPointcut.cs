using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using SheepAspect.Helpers;

namespace SheepAspect.Pointcuts.Impl
{
    public class FieldPointcut : PointcutBase<FieldPointcut>, IFieldPointcut, IWhereLiteral
    {
        private readonly IList<Func<FieldDefinition, bool>> filters = new List<Func<FieldDefinition, bool>>();
        private readonly TypePointcut typeFilter = new TypePointcut();

        public void WhereName(StringCriteria name)
        {
            Where(f => name.Match(f.Name));
        }

        public void WhereNot(FieldPointcut pointcut)
        {
            if (pointcut.filters.Any())
            {
                Where(f => !pointcut.MatchFull(f));
            }
            else
            {
                typeFilter.WhereNot(pointcut.typeFilter);
            }
        }

        public void WhereInType(TypePointcut crit)
        {
            typeFilter.WhereAny(crit);
        }

        public void WhereType(TypePointcut crit)
        {
            Where(f => crit.MatchFull(f.FieldType.Resolve()));
        }

        public void WhereHasCustomAttributeType(TypePointcut type)
        {
            Where(m => m.CustomAttributes.Any(a => type.MatchFull(a.AttributeType.Resolve())));
        }

        public void WhereStatic()
        {
            Where(f => f.IsStatic);
        }

        public void WherePublic()
        {
            Where(f => f.IsPublic);
        }

        public void WherePrivate()
        {
            Where(f => f.IsPrivate);
        }

        public void WhereProtected()
        {
            Where(m => m.IsFamily);
        }

        public void WhereInternal()
        {
            Where(m => m.IsAssembly);
        }

        public override void WhereAny(Func<FieldPointcut[]> func)
        {
            typeFilter.Where(type => func().Any(c => c.Match(type)));
            Where(field =>
                      {
                          var pointcuts = func();
                          if (pointcuts.Length == 1)
                          {
                              return pointcuts[0].Match(field);
                          }

                          return pointcuts.Any(c => c.MatchFull(field));
                      });
        }

        public void Where(Func<FieldDefinition, bool> condition)
        {
            filters.Add(condition);
        }
        
        public bool Match(TypeDefinition type)
        {
            return typeFilter.Match(type);
        }

        public bool MatchFull(TypeDefinition type)
        {
            return typeFilter.MatchFull(type);
        }

        public bool Match(FieldDefinition field)
        {
            return filters.All(f => f(field));
        }

        public bool MatchFull(FieldDefinition field)
        {
            return Match(field.DeclaringType) && filters.All(f => f(field));
        }

        public void WhereLiteral(string value)
        {
            // Need a better DSL for this
            Where(f=> f.FullName.IsWildcardMatch(value));
        }
    }
}