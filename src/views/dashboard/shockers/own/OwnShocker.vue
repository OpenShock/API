<template>
    <b-container>
        <b-row>
            <p>{{ shocker.name }}</p>
        </b-row>
        <b-row>
            <input type="range" min="1" max="100" v-model="shocker.state.intensity">
            <input type="range" min="300" max="30000" v-model="shocker.state.duration">
        </b-row>
        <b-row>
            <b-col><button @click="control(3)">Sound</button></b-col>
            <b-col><button @click="control(2)">Vibrate</button></b-col>
            <b-col><button @click="control(1)">Shock</button></b-col>
        </b-row>
    </b-container>
</template>

<script>
    export default {
        props: ["shocker"],
        data() {
            return {
                
            }
        },
        methods: {
            control(type) {
                const obj = {
                    "RequestType": 0,
                    "data": [
                        {
                            "Id": this.shocker.id,
                            "Type": type,
                            "Duration": parseInt(this.shocker.state.duration),
                            "Intensity": parseInt(this.shocker.state.intensity)
                        }
                    ]
                };
                console.log(this);
                ws.send(JSON.stringify(obj));
            }
        }
    }
</script>