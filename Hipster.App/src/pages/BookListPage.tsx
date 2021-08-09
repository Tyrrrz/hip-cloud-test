import api from "../api";
import { Link as RouterLink } from "react-router-dom";
import { useQuery } from "react-query";
import Error from "../shared/Error";
import {
  Box,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  Image,
  Link,
  Wrap,
} from "@chakra-ui/react";

export default function BookListPage() {
  const booksQuery = useQuery("all-books", () => api.books.getBooks(), {
    retry: false,
  });

  if (booksQuery.isLoading) {
    return <div>Loading...</div>;
  }

  if (booksQuery.error) {
    return (
      <div>
        Error: <Error error={booksQuery.error} />
      </div>
    );
  }

  return (
    <Box>
      <Breadcrumb>
        <BreadcrumbItem>
          <BreadcrumbLink as={RouterLink} to="/" color="blue.500">
            Home
          </BreadcrumbLink>
        </BreadcrumbItem>
      </Breadcrumb>

      <Box fontSize="3xl" marginTop="1em">
        Books
      </Box>

      <Wrap spacing="2em">
        {booksQuery.data!.map((book) => (
          <Box key={book.id}>
            <Image src={book.coverImageUrl} width="100px" />
            <Link
              as={RouterLink}
              to={`/${book.id}`}
              fontSize="xl"
              fontWeight="semibold"
              color="blue.500"
            >
              {book.title}
            </Link>
            <Box fontWeight="semibold">{book.author}</Box>
            <Box>ISBN: {book.isbn}</Box>
            <Box>Published: {book.year}</Box>
          </Box>
        ))}
      </Wrap>
    </Box>
  );
}
