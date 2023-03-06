<template>
    <b-container>
        <b-row v-for="item in ownShockers" :key="item.id">
            <device :device="item"></device>
        </b-row>
    </b-container>
</template>

<script>
    import Device from './Device'

    export default {
        components: {Device},
        data() {
            return {
                ownShockers: []
            }
        },
        async beforeMount() {
            const res = await apiCall.makeCall('GET', '1/shockers/own');
			if (res === undefined || res.status !== 200) {
				toastr.error("Error while retrieving own shockers");
				return;
			}

            this.ownShockers = res.data.data;
            this.ownShockers.forEach(device => {
                device.state = {
                    online: false
                };

                device.shockers.forEach(shocker => {
                    shocker.state = {
                        intensity: 25,
                        duration: 1000,
                        type: 2
                    }
                });
            });
            console.log(res.data);
        }
    }
</script>
