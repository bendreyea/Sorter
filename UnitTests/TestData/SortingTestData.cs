namespace App.ExternalSorter.UnitTests.TestData;

public class SortingTestData
{
    public static IEnumerable<object[]> GetSortingTestCases()
    {
        yield return new object[]
        {
            //unsorted
            new string[]
            {
                "5. Banana",
                "3. Cat",
                "2. Apple",
                "123. Pineapple",
                "32. Cherry is the best",
                "1. Apple",
                "5. Banana",
                "4. Dog",
                "15. Mango Juice",
                "6. Elephant"
            },
            //sorted
            new string[]
            {
                "1. Apple",
                "2. Apple",
                "5. Banana",
                "5. Banana",
                "3. Cat",
                "32. Cherry is the best",
                "4. Dog",
                "6. Elephant",
                "15. Mango Juice",
                "123. Pineapple",
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "102. Watermelon",
                "1. Apple Pie",
                "15. Mango Juice",
                "14. Banana Smoothie",
                "10. Apple Pie",
                "2. Kiwi Fruit"
            },
            new string[]
            {
                "1. Apple Pie",
                "10. Apple Pie",
                "14. Banana Smoothie",
                "2. Kiwi Fruit",
                "15. Mango Juice",
                "102. Watermelon"
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "4. Pear",
                "23. Orange",
                "123. Pineapple",
                "56. Grape",
                "5. Apple",
                "9. Banana",
                "100. Lemon",
                "2. Apple",
                "67. Blueberry",
                "45. Avocado",
                "9. Apricot",
                "29. Mango",
                "98. Kiwi",
                "43. Watermelon",
                "888. Apple",
                "33. Orange",
                "98. Kiwi",
                "4567. Banana",
                "1. Apple",
            },
            new string[]
            {
                "1. Apple",
                "2. Apple",
                "5. Apple",
                "888. Apple",
                "9. Apricot",
                "45. Avocado",
                "9. Banana",
                "4567. Banana",
                "67. Blueberry",
                "56. Grape",
                "98. Kiwi",
                "98. Kiwi",
                "100. Lemon",
                "29. Mango",
                "23. Orange",
                "33. Orange",
                "4. Pear",
                "123. Pineapple",
                "43. Watermelon",
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "1. Apple Pie",
                "10. apple pie",
                "15. Mango Juice",
                "  4. banana Smoothie ",
                "2. kiwi fruit",
                "3. Banana Smoothie"
            },
            new string[]
            {
                "10. apple pie",
                "1. Apple Pie",
                "3. Banana Smoothie",
                "  4. banana Smoothie ",
                "2. kiwi fruit",
                "15. Mango Juice"
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "1. Avocado",
                "1. Apple",
                "1. Apricot",
            },
            new string[]
            {
                "1. Apple",
                "1. Apricot",
                "1. Avocado"
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "10. Apple",
                "10. 10 Apples",
                "9. 9 Lives",
                "9. 10 Cats"
            },
            new string[]
            {
                "10. 10 Apples",
                "9. 10 Cats",
                "9. 9 Lives",
                "10. Apple"
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "415. Apple",
                "30432. Something something something",
                "1. Apple",
                "32. Cherry is the best",
                "2. Banana is yellow",
            },
            new string[]
            {
                "1. Apple",
                "415. Apple",
                "2. Banana is yellow",
                "32. Cherry is the best",
                "30432. Something something something",
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "001. Apple",
                "00032. Cherry",
                "002. Banana"
            },
            new string[]
            {
                "001. Apple",
                "002. Banana",
                "00032. Cherry"
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "-5. Banana is yellow",
                "-1. Apple",
                "2. Cherry is the best"
            },
            new string[]
            {
                "-1. Apple",
                "-5. Banana is yellow",
                "2. Cherry is the best"
            },
        };
        
        yield return new object[]
        {
            new string[]
            {
                "-5. Banana is yellow",
                "-1. Apple",
                "3147483647. Cherry is the best",
                "2. Cherry is the best",
            },
            new string[]
            {
                "-1. Apple",
                "-5. Banana is yellow",
                "2. Cherry is the best",
                "3147483647. Cherry is the best"
            },
        };
        
        yield return new object[]
        {
            new string[]
            {
                "2. Cherry is the best",
                "-1. Apple",
                "-5. Apple"
            },
            new string[]
            {
                "-5. Apple",
                "-1. Apple",
                "2. Cherry is the best"
            },
        };

        yield return new object[]
        {
            // wrong order if string part equals. If string equals should sort by number
            new string[]
            {
                "3. apple",
                "2. apple",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. banana"
            },
            new string[]
            {
                "2. apple",
                "3. apple",
                "1. banana",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
                "1. BANANA",
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "1. Apple",
                "1. and",
                "1. BANANA",
                "1. banana"
            },
            new string[]
            {
                "1. and",
                "1. Apple",
                "1. banana",
                "1. BANANA",
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "20. and",
                "1. and and",
                "1. and",
            },
            new string[]
            {
                "1. and",
                "20. and",
                "1. and and",
            }
        };

        yield return new object[]
        {
            new string[]
            {
                "1. Cherry!",
                "2. @pple",
                "3. $omething something",
                "4. #Banana",
            },
            new string[]
            {
                "4. #Banana",
                "3. $omething something",
                "2. @pple",
                "1. Cherry!",
            }
        };
        
        // Case with accented characters (diacritics)
        yield return new object[]
        {
            // unsorted
            new string[]
            {
                "1. Café",
                "2. Cafe",
                "3. Caffè",
                "4. Καφές", // Greek for coffee
                "5. काफ़ी", // Hindi for coffee
                "6. Café"
            },
            // sorted
            new string[]
            {
                "2. Cafe",
                "3. Caffè",
                "1. Café",
                "6. Café",
                "4. Καφές",
                "5. काफ़ी"
            }
        };

        // Large dataset with 50+ items
        yield return new object[]
        {
            new string[]
            {
                "100. Zebra", "1. Apple", "50. Mango", "25. Grape", "75. Orange",
                "10. Banana", "90. Watermelon", "5. Apricot", "45. Lemon", "85. Strawberry",
                "20. Cherry", "60. Papaya", "30. Kiwi", "70. Pineapple", "15. Blueberry",
                "95. Yam", "40. Lime", "80. Raspberry", "55. Melon", "35. Guava",
                "65. Peach", "2. Avocado", "12. Blackberry", "22. Coconut", "32. Dragonfruit",
                "42. Elderberry", "52. Fig", "62. Grapefruit", "72. Honeydew", "82. Jackfruit",
                "92. Kumquat", "3. Acai", "13. Boysenberry", "23. Cantaloupe", "33. Date",
                "43. Feijoa", "53. Gooseberry", "63. Huckleberry", "73. Jabuticaba", "83. Kiwano",
                "93. Lychee", "4. Ackee", "14. Bilberry", "24. Cloudberry", "34. Damson",
                "44. Elderflower", "54. Goumi", "64. Hawthorn", "74. Jujube", "84. Korlan",
                "94. Longan", "6. Aronia", "16. Barberry", "26. Cranberry", "36. Durian"
            },
            new string[]
            {
                "3. Acai",
                "4. Ackee",
                "1. Apple",
                "5. Apricot",
                "6. Aronia",
                "2. Avocado",
                "10. Banana",
                "16. Barberry",
                "14. Bilberry",
                "12. Blackberry",
                "15. Blueberry",
                "13. Boysenberry",
                "23. Cantaloupe",
                "20. Cherry",
                "24. Cloudberry",
                "22. Coconut",
                "26. Cranberry",
                "34. Damson",
                "33. Date",
                "32. Dragonfruit",
                "36. Durian",
                "42. Elderberry",
                "44. Elderflower",
                "43. Feijoa",
                "52. Fig",
                "53. Gooseberry",
                "54. Goumi",
                "25. Grape",
                "62. Grapefruit",
                "35. Guava",
                "64. Hawthorn",
                "72. Honeydew",
                "63. Huckleberry",
                "73. Jabuticaba",
                "82. Jackfruit",
                "74. Jujube",
                "83. Kiwano",
                "30. Kiwi",
                "84. Korlan",
                "92. Kumquat",
                "45. Lemon",
                "40. Lime",
                "94. Longan",
                "93. Lychee",
                "50. Mango",
                "55. Melon",
                "75. Orange",
                "60. Papaya",
                "65. Peach",
                "70. Pineapple",
                "80. Raspberry",
                "85. Strawberry",
                "90. Watermelon",
                "95. Yam",
                "100. Zebra"

            }
        };

        // Edge case: very large numbers - sorts by string first, then number
        yield return new object[]
        {
            new string[]
            {
                "999999999. Large",
                "1. Small",
                "500000000. Medium",
                "1000000000. Huge",
                "250000000. Quarter"
            },
            new string[]
            {
                "1000000000. Huge",
                "999999999. Large",
                "500000000. Medium",
                "250000000. Quarter",
                "1. Small"
            }
        };
        

        // Large dataset with duplicates and mixed content - sorts by string, then number
        yield return new object[]
        {
            new string[]
            {
                "5. Apple", "3. Apple", "5. Apple", "1. Banana", "10. Banana",
                "7. Cherry", "7. Cherry", "2. Date", "8. Elderberry", "4. Fig",
                "6. Grape", "9. Honeydew", "11. Ice Cream", "12. Jackfruit", "13. Kiwi",
                "14. Lemon", "15. Mango", "16. Nectarine", "17. Orange", "18. Papaya",
                "19. Quince", "20. Raspberry", "21. Strawberry", "22. Tangerine", "23. Ugli",
                "24. Vanilla", "25. Watermelon", "26. Xigua", "27. Yam", "28. Zucchini",
                "29. Apricot", "30. Blackberry", "31. Cantaloupe", "32. Dragonfruit", "33. Eggplant"
            },
            new string[]
            {
                "3. Apple", "5. Apple", "5. Apple", "29. Apricot", "1. Banana",
                "10. Banana", "30. Blackberry", "31. Cantaloupe", "7. Cherry", "7. Cherry",
                "2. Date", "32. Dragonfruit", "33. Eggplant", "8. Elderberry", "4. Fig",
                "6. Grape", "9. Honeydew", "11. Ice Cream", "12. Jackfruit", "13. Kiwi",
                "14. Lemon", "15. Mango", "16. Nectarine", "17. Orange", "18. Papaya",
                "19. Quince", "20. Raspberry", "21. Strawberry", "22. Tangerine", "23. Ugli",
                "24. Vanilla", "25. Watermelon", "26. Xigua", "27. Yam", "28. Zucchini"
            }
        };

        // Mixed case sensitivity and spacing
        yield return new object[]
        {
            new string[]
            {
                "10. APPLE", "5. apple", "10. Apple", "15. APPLE PIE", "5. Apple Pie",
                "20. banana", "10. BANANA", "30. Cherry", "25. cherry", "15. CHERRY"
            },
            new string[]
            {
                "5. apple",
                "10. Apple",
                "10. APPLE",
                "5. Apple Pie",
                "15. APPLE PIE",
                "20. banana",
                "10. BANANA",
                "25. cherry",
                "30. Cherry",
                "15. CHERRY"
            }
        };

        // Extreme: single character differences
        yield return new object[]
        {
            new string[]
            {
                "1. a", "1. b", "1. c", "1. d", "1. e", "1. f", "1. g", "1. h",
                "2. a", "2. b", "2. c", "2. d", "2. e", "2. f", "2. g", "2. h"
            },
            new string[]
            {
                "1. a",
                "2. a",
                "1. b",
                "2. b",
                "1. c",
                "2. c",
                "1. d",
                "2. d",
                "1. e",
                "2. e",
                "1. f",
                "2. f",
                "1. g",
                "2. g",
                "1. h",
                "2. h"
            }
        };
    }
}
// Case with emoji characters
// yield return new object[]
// {
//     // unsorted
//     new string[]
//     {
//         "1. 🍎 Apple",
//         "2. 🍌 Banana",
//         "3. 🍒 Cherry",
//         "4. 🥭 Mango",
//         "5. 🍓 Strawberry",
//         "6. 🍍 Pineapple"
//     },
//     // sorted
//     new string[]
//     {
//         "1. 🍎 Apple",
//         "2. 🍌 Banana",
//         "3. 🍒 Cherry",
//         "4. 🍍 Pineapple",
//         "5. 🍓 Strawberry",
//         "6. 🥭 Mango"
//     }
// };

// yield return new object[]
// {
//     new string[]
//     {
//         "1. @pple",
//         "2. #Banana",
//         "3. Cherry!",
//         "4. $omething something"
//     },
//     new string[]
//     {
//         "1. @pple",
//         "2. #Banana",
//         "3. Cherry!",
//         "4. $omething something"
//     }
// };