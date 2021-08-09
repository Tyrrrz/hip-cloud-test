import api from "../api";
import { useParams } from "react-router";
import { useQuery } from "react-query";
import Error from "../shared/Error";
import { Link as RouterLink } from "react-router-dom";
import {
  Box,
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  Image,
  Wrap,
} from "@chakra-ui/react";

export default function BookPage() {
  const { bookId } = useParams<{ bookId?: string }>();

  const bookQuery = useQuery(
    `book-${bookId}`,
    () => api.books.getBook(Number(bookId)),
    {
      retry: false,
    }
  );

  if (bookQuery.isLoading) {
    return <div>Loading...</div>;
  }

  if (bookQuery.error) {
    return (
      <div>
        Error: <Error error={bookQuery.error} />
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

        <BreadcrumbItem>
          <BreadcrumbLink
            as={RouterLink}
            to={`/${bookQuery.data!.id}`}
            color="blue.500"
          >
            {bookQuery.data!.title}
          </BreadcrumbLink>
        </BreadcrumbItem>
      </Breadcrumb>

      <Box fontSize="3xl" marginTop="1em">
        {bookQuery.data!.title}
      </Box>

      <Wrap spacing="2em">
        <Box key={bookQuery.data!.id}>
          <Image src={bookQuery.data!.coverImageUrl} width="300px" />
          <Box fontWeight="semibold">{bookQuery.data!.author}</Box>
          <Box>ISBN: {bookQuery.data!.isbn}</Box>
          <Box>Published: {bookQuery.data!.year}</Box>
        </Box>
      </Wrap>
    </Box>
  );
}
