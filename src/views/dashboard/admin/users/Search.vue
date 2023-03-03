<template>
  <div class="col base-widget">
    <div class="row" style="height: 100%;">
      <form class="col">
        <label for="searchTerm">Search Term</label>
        <input type="search" class="form-control" id="searchTerm" placeholder="Enter search term"
               v-model="options.term">
      </form>
      <button class="col btn btn-nano scuff" @click="search">
        <span v-show="loading"><i class="fas fa-cog fa-spin"></i></span>
        <span v-show="!loading"><i class="fas fa-search"></i></span>
      </button>
      <div class="col-8 table-div">
        <table class="table">
          <thead>
          <tr>
            <th scope="col">Username</th>
            <th scope="col">Email</th>
            <th scope="col">Created</th>
          </tr>
          </thead>
          <tbody>
          <tr v-for="item in searchResults" @click="clicked(item.Id)">
            <td>{{ item.Username }}</td>
            <td>{{ item.Email }}</td>
            <td>{{ item.CreationDate }}</td>
          </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      searchResults: [],
      options: {
        term: "",
        page: 1,
        perPage: 18
      },
      loading: false
    }
  },
  methods: {
    async search() {
      this.loading = true;
      const res = await apiCall.makeCall('POST', `admin/users/search?offset=${this.start}&count=${this.options.perPage}`, {
        "term": this.options.term
      });
      this.loading = false;
      this.searchResults = res.data.Data.Results;
    },
    clicked(userId) {
      this.emitter.emit('getUser', userId);
    }
  },
  watch: {
    'options.page': function (newV, oldV) {
      this.getAll();
    },
  },
  computed: {
    start() {
      return this.options.page * this.options.perPage - this.options.perPage;
    }
  }
}
</script>

<style scoped lang="scss">
.scuff {
  flex: 0 0 auto;
  width: 39px;
}

.table-div {
  height: 100%;
  overflow: auto;

  .table {
    margin-bottom: 0;
  }
}
</style>
