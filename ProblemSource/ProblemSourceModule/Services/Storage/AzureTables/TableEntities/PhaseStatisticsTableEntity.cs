﻿using AzureTableGenerics;
using ProblemSource.Models.Aggregates;

namespace ProblemSource.Services.Storage.AzureTables.TableEntities
{
    internal class PhaseStatisticsTableEntity : TableEntityBase
    {
        public int id { get; set; }
        public int phase_id { get; set; }
        public int account_id { get; set; }

        public int training_day { get; set; }
        public string exercise { get; set; } = string.Empty;
        public string phase_type { get; set; } = string.Empty;
        public DateTime timestamp { get; set; }
        public DateTime end_timestamp { get; set; }

        public int sequence { get; set; }

        public int num_questions { get; set; }
        public int num_correct_first_try { get; set; }
        public int num_correct_answers { get; set; }
        public int num_incorrect_answers { get; set; }

        public double level_min { get; set; }  // TableEntity doesn't support decimal
        public double level_max { get; set; }

        public int response_time_avg { get; set; }
        public int response_time_total { get; set; }

        public bool? won_race { get; set; }
        public bool? completed_planet { get; set; }

        public PhaseStatistics ToBusinessObject()
        {
            return new PhaseStatistics
            {
                //id
                // phase_id
                account_id = account_id,
                training_day = training_day,
                exercise = exercise,
                phase_type = phase_type,
                timestamp = new DateTime(timestamp.Ticks, DateTimeKind.Utc),
                end_timestamp = new DateTime(end_timestamp.Ticks, DateTimeKind.Utc),

                sequence = sequence,

                num_questions = num_questions,
                num_correct_first_try = num_correct_first_try,
                num_correct_answers = num_correct_answers,
                num_incorrect_answers = num_incorrect_answers,

                level_min = (decimal)level_min, // TableEntity doesn't support decimal
                level_max = (decimal)level_max,

                response_time_avg = response_time_avg,
                response_time_total = response_time_total,

                won_race = won_race,
                completed_planet = completed_planet,
            };
        }

        public static PhaseStatisticsTableEntity FromBusinessObject(PhaseStatistics p, int userId) => new PhaseStatisticsTableEntity
        {
            //id
            // phase_id
            account_id = p.account_id,
            training_day = p.training_day,
            exercise = p.exercise,
            phase_type = p.phase_type,
            timestamp = new DateTime(p.timestamp.Ticks, DateTimeKind.Utc),
            end_timestamp = new DateTime(p.end_timestamp.Ticks, DateTimeKind.Utc),

            sequence = p.sequence,

            num_questions = p.num_questions,
            num_correct_first_try = p.num_correct_first_try,
            num_correct_answers = p.num_correct_answers,
            num_incorrect_answers = p.num_incorrect_answers,

            level_min = (double)p.level_min, // TableEntity doesn't support decimal
            level_max = (double)p.level_max,

            response_time_avg = p.response_time_avg,
            response_time_total = p.response_time_total,

            won_race = p.won_race,
            completed_planet = p.completed_planet,

            PartitionKey = AzureTableConfig.IdToKey(userId),
            RowKey = PhaseStatistics.UniqueIdWithinUser(p),
        };
    }
}
