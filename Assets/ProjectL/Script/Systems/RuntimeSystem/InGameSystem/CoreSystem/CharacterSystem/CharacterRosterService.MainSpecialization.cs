using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal sealed partial class CharacterRosterService
    {
        internal MainSpecializationOperationPreparation PrepareMainSpecialization(RunId expectedRun,
            long expectedRosterRevision, CharacterInstanceId mainId, CategoryKey selected)
        {
            RequireGrowthReady();
            MainSpecializationOperationPreparation Failure(MainSpecializationFailureReason reason) =>
                new(this, MainSpecializationDecision.Failure(reason));
            if (_buffers.RunProgressReadSink.RunId.IsEmpty || RunEnded) return Failure(MainSpecializationFailureReason.RunUnavailable);
            if (_buffers.RunProgressReadSink.RunId != expectedRun || _buffers.CharacterReadSink.RosterRevision != expectedRosterRevision)
                return Failure(MainSpecializationFailureReason.SourceChanged);
            if (InBattle) return Failure(MainSpecializationFailureReason.InBattle);
            var source = new MainSpecializationSource(_buffers);
            if (source.Deck == null || source.Deck.RunId != expectedRun || source.Deck.MainId != mainId ||
                source.Main == null || source.Main.CurrentHealth <= 0 || !_buffers.CharacterReadSink.TryGetOwned(mainId, out var owned))
                return Failure(MainSpecializationFailureReason.InvalidTarget);
            var decision = MainSpecializationRules.Plan(owned, selected);
            if (!decision.Succeeded) return new MainSpecializationOperationPreparation(this, decision);
            MainSpecializationDefinitionSnapshot definitions;
            try { definitions = ReadMainSpecializationDefinitions(owned); }
            catch (ArgumentNullException) { throw; }
            catch (ArgumentException) { return Failure(source.Matches(_buffers) ? MainSpecializationFailureReason.InvalidDefinition : MainSpecializationFailureReason.SourceChanged); }
            catch (InvalidOperationException) { return Failure(source.Matches(_buffers) ? MainSpecializationFailureReason.InvalidDefinition : MainSpecializationFailureReason.SourceChanged); }
            try
            {
                TraitGrowthCatalogValidator.ValidateSpecialization(definitions.Catalog, definitions.Profile, selected);
                _ = new CombatAttackProfile(OwnedUnitSpecResolver.Resolve(definitions.Profile.Definition,
                    decision.Change.Final, definitions.Catalog), definitions.Profile.Method);
            }
            catch (ArgumentNullException) { throw; }
            catch (ArgumentException) { return Failure(source.Matches(_buffers) ? MainSpecializationFailureReason.InvalidDefinition : MainSpecializationFailureReason.SourceChanged); }
            catch (InvalidOperationException) { return Failure(MainSpecializationFailureReason.UnsupportedProgression); }
            TraitGrowthProgressSnapshot growth;
            try { growth = ReadTraitGrowthProgress(); }
            catch (InvalidOperationException)
            {
                if (!source.Matches(_buffers)) return Failure(MainSpecializationFailureReason.SourceChanged);
                throw;
            }
            if (!source.Matches(_buffers)) return Failure(MainSpecializationFailureReason.SourceChanged);
            if (growth.Status != TraitGrowthProgressStatus.ReadyToContinue) return Failure(MainSpecializationFailureReason.GrowthPending);
            var prepared = new MainSpecializationOperationPreparation(this, decision, source, definitions, growth);
            return MainSpecializationSourceMatches(prepared) ? prepared : Failure(MainSpecializationFailureReason.SourceChanged);
        }

        internal MainSpecializationDecision ApplyMainSpecialization(MainSpecializationOperationPreparation prepared)
        {
            if (prepared == null) throw new ArgumentNullException(nameof(prepared));
            RequireGrowthReady();
            if (prepared.Owner != this || prepared.Installed) return MainSpecializationDecision.Failure(MainSpecializationFailureReason.SourceChanged);
            if (!prepared.Decision.Succeeded) return prepared.Decision;
            if (!MainSpecializationSourceMatches(prepared)) return MainSpecializationDecision.Failure(MainSpecializationFailureReason.SourceChanged);
            PreparedMainSpecializationApplication application;
            try
            {
                application = _buffers.MainSpecializationOperations.Prepare(prepared.Source.RunId, prepared.Source.Revision, prepared.Decision.Change);
            }
            catch (OverflowException) { return MainSpecializationDecision.Failure(MainSpecializationFailureReason.SourceChanged); }
            catch (InvalidOperationException) { return MainSpecializationDecision.Failure(MainSpecializationFailureReason.SourceChanged); }
            // Buffer가 새로 포착한 문맥으로 옛 Core 결정을 정당화하지 않는다.
            if (!MainSpecializationSourceMatches(prepared)) return MainSpecializationDecision.Failure(MainSpecializationFailureReason.SourceChanged);
            _buffers.MainSpecializationOperations.Install(application);
            prepared.MarkInstalled();
            return prepared.Decision;
        }

        private MainSpecializationDefinitionSnapshot ReadMainSpecializationDefinitions(OwnedCharacterRecord owned)
        {
            var config = _query.CategoryConfigTableQuery.Config ?? throw new ArgumentNullException("categoryConfig");
            config.Validate();
            var units = _buffers.CharacterReadSink.Owned.Select(o => o.CharacterKey).Distinct().OrderBy(k => k)
                .Select(k => UnitSpecAdapter.ReadUnit(_units.GetUnit(k))).ToArray();
            var profile = ReadProfile(owned);
            PlayerMainDefinitionPolicy.Validate(profile.Definition.Key, profile.Definition.Role,
                profile.Definition.CategoryKey, profile.Definition.OwnerEligibility);
            return new MainSpecializationDefinitionSnapshot(profile, ReadCatalog(), config, units);
        }

        private bool MainSpecializationSourceMatches(MainSpecializationOperationPreparation prepared)
        {
            if (!prepared.Source.Matches(_buffers)) return false;
            try
            {
                if (!prepared.Definitions.SameValues(ReadMainSpecializationDefinitions(prepared.Decision.Change.Expected))) return false;
                if (ReadTraitGrowthProgress().Status != TraitGrowthProgressStatus.ReadyToContinue) return false;
                // 성장 조회 역시 DB를 읽으므로 그 동안 변경된 정의도 다시 확인한다.
                return prepared.Definitions.SameValues(ReadMainSpecializationDefinitions(prepared.Decision.Change.Expected)) && prepared.Source.Matches(_buffers);
            }
            catch (ArgumentNullException) { throw; }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
            catch (System.Collections.Generic.KeyNotFoundException) { return false; }
        }
    }
}
