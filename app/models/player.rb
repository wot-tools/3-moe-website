class Player < ApplicationRecord
	belongs_to :clan
	has_many :marks
end
