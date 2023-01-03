<script lang="ts">
	import type { TrainingSummary } from 'src/apiClient';
	import TrainingsTable from '../components/trainingsTable.svelte';

	let trainingSummaries: TrainingSummary[] = [
        { id: 1, days: [
            { accountId: 1, accountUuid: "aa", trainingDay: 1, startTime: new Date("2023-01-01"), endTimeStamp: new Date(), 
            numRacesWon: 1, numRaces: 1, numPlanetsWon: 1, numCorrectAnswers: 1, numQuestions: 1, responseMinutes:8, remainingMinutes:4 }] },
		{ id: 2, days: [
            { accountId: 1, accountUuid: "ab", trainingDay: 1, startTime: new Date("2023-01-02"), endTimeStamp: new Date(), 
            numRacesWon: 1, numRaces: 1, numPlanetsWon: 1, numCorrectAnswers: 1, numQuestions: 1, responseMinutes:8, remainingMinutes:4 }] },
		{ id: 3, days: [
            { accountId: 1, accountUuid: "ac", trainingDay: 1, startTime: new Date("2023-01-03"), endTimeStamp: new Date(), 
            numRacesWon: 1, numRaces: 1, numPlanetsWon: 1, numCorrectAnswers: 1, numQuestions: 1, responseMinutes:8, remainingMinutes:4 }] },
    ];

</script>

<!-- <TrainingView></TrainingView> -->	

<TrainingsTable trainingSummaries={trainingSummaries} numDaysBack={5}></TrainingsTable>