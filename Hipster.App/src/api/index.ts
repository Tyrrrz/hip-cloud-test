import * as generated from "./generated";
import config from "./config";

const clients = {
    books: new generated.BooksClient(config.apiUrl)
}

export default clients;