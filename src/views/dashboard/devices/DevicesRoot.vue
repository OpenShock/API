<template>
  <div class="base-wrap">
      <div class="add-circle" @click="createNewDevice">
        <i class="fa-solid fa-plus"></i>
      </div>

      <b-container>
      <b-table hover striped :items="devices" :fields="fields" class="devices-table">
        <template #cell(actions)="row">
          <b-button size="sm" @click="deleteDevice(row.item)" class="mr-2">
            <i class="fa-solid fa-trash"></i> Delete
          </b-button>
        </template>
      </b-table>
    </b-container>

      <b-modal v-model="modal.edit" title="Edit Device">
        <p class="my-4">Hello from modal!</p>
      </b-modal>

      <b-modal v-model="modal.delete" title="Delete Device?" cancel-variant="success" ok-title="Delete" ok-variant="danger" @ok="confirmedDeleteDevice">
        <p>You are about to delete device <b>{{ lastDeleteItem.name }}</b> with id ({{ lastDeleteItem.id }}).</p>

        <p>This will also delete <b>all shocker configurations and shares associated with that shocker.
        </b><br><br><b>This is permanent and cannot be undone.</b> Are you sure?</p>

      </b-modal>
  </div>
</template>

<script>
  export default {
    data() {
      return {
        fields: ['name', 'actions'],
        devices: [],
        lastDeleteItem: {
          name: "",
          id: ""
        },
        modal: {
          delete: false,
          edit: false
        }
      }
    },
    mounted() {

    },
    async beforeMount() {
        await this.loadDevices();
    },
    methods: {
      deleteDevice(item) {
        this.lastDeleteItem = item;
        this.modal.delete = true;
      },
      async confirmedDeleteDevice() {
        const res = await apiCall.makeCall('DELETE', '1/devices/' + this.lastDeleteItem.id);
        if (res === undefined || res.status !== 200) {
          toastr.error("Error while deleting device");
          return;
        }
        toastr.success("Successfully deleted device (" + this.lastDeleteItem.name + ")[" + this.lastDeleteItem.id + "]")
        await this.loadDevices();
      },
      async createNewDevice() {
        const res = await apiCall.makeCall('POST', '1/devices');
        if (res === undefined || res.status !== 201) {
          toastr.error("Error while creating new device");
          return;
        }
        
        toastr.success("Successfully created new device");
        await this.loadDevices();
      },
      async loadDevices() {
        const res = await apiCall.makeCall('GET', '1/devices');
        if (res === undefined || res.status !== 200) {
          toastr.error("Error while retrieving devices");
          return;
        }

        this.devices = res.data.data;
      }
    }
  }
</script>

<style scoped lang="scss">
.add-circle {
  position: fixed;
  right: 10px;
  bottom: 10px;
  width: 60px;
  height: 60px;


  background-color: #7ac142;
  border-radius: 50%;
  cursor: pointer;

  transition: background-color 0.2s;
  &:hover {
    background-color: #5e9634;
  }

  svg {
    height: 40px;
    width: 40px;

    position: relative;
    left: 52%;
    transform: translateX(-50%) translateY(-50%);
    top: 50%;

  }
}

.devices-table {
  
}
</style>