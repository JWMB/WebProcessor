<script lang="ts">
	import type { TrainingSummary } from 'src/apiClient';
	import TrainingsTable from '../components/trainingsTable.svelte';

	let trainingSummaries: TrainingSummary[] = [
        { id: 1, days: [
            { accountId: 1, accountUuid: "aa", trainingDay: 1, startTime: new Date("2023-01-01"), endTimeStamp: new Date(), 
            numRacesWon: 1, numRaces: 1, numPlanetsWon: 1, numCorrectAnswers: 1, numQuestions: 1, responseMinutes:8, remainingMinutes:4 }] }
    ];

</script>

<!-- <TrainingView></TrainingView> -->

<TrainingsTable></TrainingsTable>