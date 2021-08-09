import { Route, Switch } from "react-router";
import { BrowserRouter } from "react-router-dom";
import BookPage from "./pages/BookPage";
import BookListPage from "./pages/BookListPage";
import { Box, Center, Heading } from "@chakra-ui/react";

function PageRouter() {
  return (
    <BrowserRouter>
      <Switch>
        <Route exact path="/:bookId" component={BookPage} />
        <Route exact path="/" component={BookListPage} />
      </Switch>
    </BrowserRouter>
  );
}

export default function App() {
  return (
    <Box>
      <Center as="header" padding={5} bg="beige">
        <Heading>Hipster Books</Heading>
      </Center>

      <Box as="main" padding={5} width="50%" margin="auto">
        <PageRouter />
      </Box>
    </Box>
  );
}
