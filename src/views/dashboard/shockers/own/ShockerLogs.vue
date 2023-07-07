<template>
    <div>
        <b-container class="logs-container">
            <b-table hover striped :items="logs" :fields="fields" class="logs-table">

                <template #cell(controlled-by)="row">
                    <b-container>
                        <b-row align-h="start" align-v="center">
                            <b-col md="auto">
                                <img class="user-image" :src="row.item.controlledBy.image + 'x128'" />
                            </b-col>
                            <b-col>
                                <p class="mb-0">{{ row.item.controlledBy.name }}</p>
                            </b-col>
                        </b-row>
                    </b-container>
                </template>

                <template #cell(type)="row">
                    <i class="fa-solid " :class="getTypeForLog(row.item.type)"></i>
                </template>

                <template #cell(intensity)="row">
                    <span>{{ row.item.intensity }}</span>
                </template>

                <template #cell(duration)="row">
                    <span>{{ row.item.duration / 1000 }}s </span>
                </template>

                <template #cell(time)="row">
                    <span>{{ new Date(row.item.createdOn).toLocaleString() }} </span>
                </template>
            </b-table>

            <loading-with-text :loading="!requestDone">Loading shocker logs...</loading-with-text>
        </b-container>
    </div>
</template>

<script>
import LoadingWithText from '@/views/utils/LoadingWithText';

export default {
  components: { LoadingWithText },
    data() {
        return {
            logs: [],
            fields: [
                {
                    key: "controlled-by",
                    label: "By User"
                },
                {
                    key: "type"
                },
                {
                    key: "intensity"
                },
                {
                    key: "duration"
                },
                {
                    key: "time"
                }
            ],
            requestDone: false,
        }
    },
    beforeMount() {
        this.getLogs();
    },
    methods: {
        async getLogs() {
            const res = await apiCall.makeCall('GET', `1/shockers/${this.$route.params.id}/logs`);
            if (res === undefined || res.status !== 200) {
                toastr.error("Error while retrieving shocker logs");
                return;
            }

            this.logs = res.data.data;
            this.requestDone = true;
        },
        getTypeForLog(type) {
            switch(type) {
                case 0: return "fa-stop";
                case 1: return "fa-bolt";
                case 2: return "fa-water";
                case 3: return "fa-volume-high";
            }
            return "fa-error"
        }
    }
}
</script>

<style scoped lang="scss">

.logs-container {
    overflow-y: scroll;
.logs-table {


    :deep(td) {
        vertical-align: middle;
    }
}
}

</style>